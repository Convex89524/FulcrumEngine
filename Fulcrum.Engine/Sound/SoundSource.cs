// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3 (GPLv3 only).
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Fulcrum.Engine.Sound
{
    public enum SoundState
    {
        Stopped,
        Playing,
        Paused
    }

    public sealed class SoundSource : IDisposable
    {
        private readonly object _sync = new();
        private readonly ILogger _log;
        private readonly IAudioDecoder _decoder;
        private readonly WaveFormat _engineFormat;
        private readonly ISampleProvider _decoded;
        private readonly ISampleProvider _toEngine;

        private VolumeSampleProvider _volume;
        private SpeedSampleProvider _speedProvider;
        private ISampleProvider _final;

        private float _playbackSpeed = 1.0f;

        private SpatialParams? _spatial;
        private Func<ListenerState>? _listenerGetter;

        public string Name { get; }
        public SoundState State { get; private set; } = SoundState.Stopped;
        public bool Loop { get; set; }

        /// <summary>音量 0~4</summary>
        public float Volume
        {
            get => _volume.Volume;
            set => _volume.Volume = Math.Clamp(value, 0f, 4f);
        }

        /// <summary> 播放速度 </summary>
        public float Speed
        {
            get => _playbackSpeed;
            set
            {
                if (value <= 0f) value = 0.01f;
                _playbackSpeed = value;
                if (_speedProvider != null)
                    _speedProvider.Speed = _playbackSpeed;
            }
        }

        public SoundSource(string name, IAudioDecoder decoder, WaveFormat engineFormat, ILogger log)
        {
            Name = name;
            _log = log;
            _decoder = decoder;
            _engineFormat = engineFormat;

            _decoded = decoder.ToSampleProvider();

            _toEngine = _decoded.WaveFormat.SampleRate == engineFormat.SampleRate
                ? _decoded
                : new WdlResamplingSampleProvider(_decoded, engineFormat.SampleRate);

            _speedProvider = new SpeedSampleProvider(_toEngine)
            {
                Speed = _playbackSpeed
            };

            _volume = new VolumeSampleProvider(_speedProvider);
            _final = _volume;

            State = SoundState.Stopped;
        }

        public void Enable3D(SpatialParams p, Func<ListenerState> listenerGetter)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (listenerGetter == null) throw new ArgumentNullException(nameof(listenerGetter));

            _spatial = p;
            _listenerGetter = listenerGetter;

            ISampleProvider baseProvider = _toEngine;

            if (baseProvider.WaveFormat.Channels != 1)
            {
                baseProvider = new StereoToMonoSampleProvider(baseProvider)
                {
                    LeftVolume = 0.5f,
                    RightVolume = 0.5f
                };
            }

            if (baseProvider.WaveFormat.SampleRate != _engineFormat.SampleRate)
            {
                baseProvider = new WdlResamplingSampleProvider(baseProvider, _engineFormat.SampleRate);
            }

            var spatial = new SpatializedProvider(baseProvider, p, listenerGetter, _engineFormat);

            float currentVol = _volume?.Volume ?? 1.0f;

            _speedProvider = new SpeedSampleProvider(spatial)
            {
                Speed = _playbackSpeed
            };

            _volume = new VolumeSampleProvider(_speedProvider)
            {
                Volume = currentVol
            };

            _final = _volume;
        }

        internal ISampleProvider Output => new LoopingProvider(this);

        private sealed class SpeedSampleProvider : ISampleProvider
        {
            private readonly ISampleProvider _source;
            private float[] _tempBuffer = Array.Empty<float>();

            public float Speed { get; set; } = 1.0f;

            public SpeedSampleProvider(ISampleProvider source)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));
            }

            public WaveFormat WaveFormat => _source.WaveFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (count == 0) return 0;

                int channels = WaveFormat.Channels;
                if (channels <= 0) return 0;

                int outFrames = count / channels;
                if (outFrames == 0) return 0;

                float spd = Speed;
                if (spd <= 0f) spd = 0.01f;

                int neededInFrames = (int)Math.Ceiling(outFrames * spd);
                int neededInSamples = neededInFrames * channels;

                if (neededInSamples <= 0)
                {
                    Array.Clear(buffer, offset, count);
                    return 0;
                }

                if (_tempBuffer.Length < neededInSamples)
                    _tempBuffer = new float[neededInSamples];

                int read = _source.Read(_tempBuffer, 0, neededInSamples);
                int readFrames = read / channels;
                if (readFrames == 0)
                {
                    Array.Clear(buffer, offset, count);
                    return 0;
                }

                int actualOutFrames = outFrames;

                if (spd >= 1.0f)
                {
                    int maxFrames = (int)Math.Floor((readFrames - 1) / spd);
                    if (maxFrames < actualOutFrames)
                        actualOutFrames = Math.Max(0, maxFrames);
                }

                int samplesWritten = 0;

                for (int outFrame = 0; outFrame < actualOutFrames; outFrame++)
                {
                    int inFrame = (int)(outFrame * spd);
                    if (inFrame >= readFrames) inFrame = readFrames - 1;

                    int inIndex = inFrame * channels;
                    int outIndex = offset + outFrame * channels;

                    for (int ch = 0; ch < channels; ch++)
                    {
                        buffer[outIndex + ch] = _tempBuffer[inIndex + ch];
                    }

                    samplesWritten += channels;
                }

                if (samplesWritten < count)
                    Array.Clear(buffer, offset + samplesWritten, count - samplesWritten);

                return samplesWritten;
            }
        }

        private sealed class LoopingProvider : ISampleProvider
        {
            private readonly SoundSource _owner;

            public LoopingProvider(SoundSource owner)
            {
                _owner = owner;
            }

            public WaveFormat WaveFormat => _owner._engineFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                if (_owner.State != SoundState.Playing)
                {
                    Array.Clear(buffer, offset, count);
                    return 0;
                }

                int total = 0;
                while (total < count)
                {
                    int n = _owner._final.Read(buffer, offset + total, count - total);
                    if (n == 0)
                    {
                        if (_owner.Loop)
                        {
                            try
                            {
                                _owner._decoder.Seek(TimeSpan.Zero);
                            }
                            catch
                            {
                                // ignore
                            }

                            continue;
                        }
                        else
                        {
                            Array.Clear(buffer, offset + total, count - total);
                            _owner.State = SoundState.Stopped;
                            break;
                        }
                    }

                    total += n;
                }

                return total;
            }
        }

        public void Play()
        {
            lock (_sync)
            {
                State = SoundState.Playing;
            }
        }

        public void Pause()
        {
            lock (_sync)
            {
                if (State == SoundState.Playing)
                    State = SoundState.Paused;
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                State = SoundState.Stopped;
                try { _decoder.Seek(TimeSpan.Zero); } catch { /* no-op */ }
            }
        }

        public void Dispose()
        {
            Stop();
            _decoder.Dispose();
        }
    }
}
