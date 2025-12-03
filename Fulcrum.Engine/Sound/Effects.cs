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
using NAudio.Dsp;
using NAudio.Wave;

namespace Fulcrum.Engine.Sound
{
    /// <summary>
    /// 单一双二阶 IIR 滤波器效果（Biquad）
    /// </summary>
    public sealed class BiquadFilterEffect : IAudioEffect
    {
        public ISampleProvider Source { get; private set; }
        public WaveFormat WaveFormat => Source.WaveFormat;

        private BiQuadFilter _left, _right;

        public BiquadFilterEffect(ISampleProvider src, BiQuadFilter left, BiQuadFilter right)
        {
            Source = src;
            _left = left; _right = right;
        }

        public void SetSource(ISampleProvider source)
        {
            Source = source;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = Source.Read(buffer, offset, count);
            int channels = WaveFormat.Channels;
            if (channels == 1)
            {
                for (int i = 0; i < read; i++)
                    buffer[offset + i] = _left.Transform(buffer[offset + i]);
            }
            else
            {
                for (int i = 0; i < read; i += channels)
                {
                    buffer[offset + i]     = _left.Transform(buffer[offset + i]);
                    buffer[offset + i + 1] = _right.Transform(buffer[offset + i + 1]);
                }
            }
            return read;
        }

        public void Dispose() { }
    }

    /// <summary>
    /// 板式混响
    /// </summary>
    public sealed class SimpleReverbEffect : IAudioEffect
    {
        public ISampleProvider Source { get; private set; }
        public WaveFormat WaveFormat => Source.WaveFormat;

        private readonly float _mix;
        private readonly float[] _delay;
        private int _idx;

        public SimpleReverbEffect(ISampleProvider src, float roomMs = 120f, float mix = 0.2f)
        {
            Source = src;
            _mix = Math.Clamp(mix, 0f, 1f);
            int samples = (int)(src.WaveFormat.SampleRate * (roomMs / 1000f));
            _delay = new float[Math.Max(1, samples)];
        }

        public void SetSource(ISampleProvider source) { /* for hot-swap */ Source = source; }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = Source.Read(buffer, offset, count);
            for (int i = 0; i < read; i++)
            {
                float dry = buffer[offset + i];
                float wet = _delay[_idx];
                float val = dry * (1f - _mix) + wet * _mix;
                buffer[offset + i] = val;

                _delay[_idx] = dry + wet * 0.35f;
                _idx++; if (_idx >= _delay.Length) _idx = 0;
            }
            return read;
        }

        public void Dispose() { }
    }
}
