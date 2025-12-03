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
using System.Numerics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Fulcrum.Engine.Sound
{
    /// <summary>
    /// 3D 空间参数
    /// </summary>
    public sealed class SpatialParams
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Velocity = Vector3.Zero;
        public float MinDistance = 1.0f;
        public float MaxDistance = 64.0f;
        public float Rolloff = 1.0f;
        public float PanStrength = 1.0f;
    }

    /// <summary>
    /// 空间化封装（单声道输入 -> 立体声输出）
    /// </summary>
    public sealed class SpatializedProvider : ISampleProvider
    {
        private readonly WaveFormat _format;
        private readonly ISampleProvider _src;
        private readonly SpatialParams _p;
        private readonly Func<ListenerState> _listenerGetter;

        private float[] _monoBuffer = Array.Empty<float>();

        public SpatializedProvider(
            ISampleProvider monoSource,
            SpatialParams p,
            Func<ListenerState> listenerGetter,
            WaveFormat engineFormat)
        {
            if (monoSource == null)
                throw new ArgumentNullException(nameof(monoSource));
            if (p == null)
                throw new ArgumentNullException(nameof(p));
            if (listenerGetter == null)
                throw new ArgumentNullException(nameof(listenerGetter));
            if (monoSource.WaveFormat.Channels != 1)
                throw new ArgumentException("SpatializedProvider expects mono source", nameof(monoSource));
            if (engineFormat == null)
                throw new ArgumentNullException(nameof(engineFormat));
            if (engineFormat.Channels != 2)
                throw new ArgumentException("Engine format must be stereo", nameof(engineFormat));

            _src = monoSource;
            _p = p;
            _listenerGetter = listenerGetter;
            _format = engineFormat;
        }

        public WaveFormat WaveFormat => _format;

        public int Read(float[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            const int channels = 2;
            int requestedFrames = count / channels;
            if (requestedFrames <= 0)
                return 0;

            if (_monoBuffer.Length < requestedFrames)
                _monoBuffer = new float[requestedFrames];

            // 读取单声道数据
            int monoRead = _src.Read(_monoBuffer, 0, requestedFrames);
            if (monoRead <= 0)
                return 0;

            int frames = monoRead;
            int samplesToWrite = frames * channels;

            var listener = _listenerGetter();
            var rel = _p.Position - listener.Position;
            float dist = rel.Length();
            
            if (dist < 1e-4f) dist = 1e-4f;

            // 计算衰减
            float atten = CalculateAttenuation(dist);

            // 计算声像
            Vector3 right = Vector3.Cross(listener.Forward, listener.Up);
            if (right.LengthSquared() > 1e-6f)
                right = Vector3.Normalize(right);
            else
                right = new Vector3(1, 0, 0);

            Vector3 relDir = rel / dist;
            float pan = Vector3.Dot(relDir, right) * _p.PanStrength;
            pan = Math.Clamp(pan, -1f, 1f);

            float leftGain = CalculatePanGain(pan, false);
            float rightGain = CalculatePanGain(pan, true);

            int writeIndex = offset;
            for (int i = 0; i < frames; i++)
            {
                float sample = _monoBuffer[i];
                
                sample *= atten;
                
                sample = Math.Clamp(sample, -1.0f, 1.0f);
                
                buffer[writeIndex] = sample * leftGain;
                buffer[writeIndex + 1] = sample * rightGain;
                writeIndex += channels;
            }

            return samplesToWrite;
        }

        private float CalculateAttenuation(float distance)
        {
            if (distance <= _p.MinDistance)
            {
                return 1.0f;
            }
            else if (distance >= _p.MaxDistance)
            {
                return 0.0f;
            }
            else
            {
                float d = distance - _p.MinDistance;
                float range = _p.MaxDistance - _p.MinDistance;
                float t = d / range;
                
                float attenuation = 1.0f - (float)Math.Pow(t, _p.Rolloff);
                return Math.Clamp(attenuation, 0.0f, 1.0f);
            }
        }

        private float CalculatePanGain(float pan, bool isRightChannel)
        {
            if (isRightChannel)
            {
                return (float)Math.Cos((1.0 - pan) * Math.PI * 0.25);
            }
            else
            {
                return (float)Math.Cos((1.0 + pan) * Math.PI * 0.25);
            }
        }
    }
}
