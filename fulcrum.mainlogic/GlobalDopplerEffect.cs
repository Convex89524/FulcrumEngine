﻿// Copyright (C) 2025-2029 Convex89524
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
using Fulcrum.Engine.Sound;
using NAudio.Wave;

namespace fulcrum.mainlogic
{
    public sealed class GlobalDopplerEffect : IAudioEffect
    {
        public ISampleProvider Source { get; private set; }
        public WaveFormat WaveFormat => Source.WaveFormat;

        private readonly Func<ListenerState> _getListener;

        public float SpeedOfSound { get; set; } = 343f;

        public float DopplerScale { get; set; } = 1.0f;

        public Vector3 DominantDirection { get; set; } = new Vector3(0, 0, -1);

        public float SmoothingFactor { get; set; } = 0.2f;

        private float[] _tempBuffer = Array.Empty<float>();
        private float _prevPitchRatio = 1.0f;

        public GlobalDopplerEffect(ISampleProvider source, Func<ListenerState> getListener)
        {
            Source       = source ?? throw new ArgumentNullException(nameof(source));
            _getListener = getListener ?? throw new ArgumentNullException(nameof(getListener));
        }

        public void SetSource(ISampleProvider source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _prevPitchRatio = 1.0f;
        }

        public void Dispose()
        {
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (Source == null || buffer == null)
                return 0;

            int channels = WaveFormat.Channels;
            if (channels <= 0)
                return 0;

            int framesOut = count / channels;
            if (framesOut <= 0)
                return 0;

            float targetRatio = ComputeDopplerRatio();
            targetRatio = Math.Clamp(targetRatio, 0.3f, 3.0f);

            float a = Math.Clamp(SmoothingFactor, 0f, 1f);
            float pitchRatio = _prevPitchRatio + (targetRatio - _prevPitchRatio) * a;
            _prevPitchRatio = pitchRatio;

            int framesNeeded   = (int)(framesOut * pitchRatio) + 2;
            int samplesNeeded  = framesNeeded * channels;

            EnsureTempBuffer(samplesNeeded);

            int readSamples = Source.Read(_tempBuffer, 0, samplesNeeded);
            if (readSamples <= channels)
            {
                Array.Clear(buffer, offset, count);
                return 0;
            }

            int framesAvail = readSamples / channels;
            if (framesAvail < 2)
            {
                Array.Clear(buffer, offset, count);
                return 0;
            }

            int framesToOutput = framesOut;
            float maxSrcFrame  = framesAvail - 1;

            for (int i = 0; i < framesToOutput; i++)
            {
                float srcPos = i * pitchRatio;

                if (srcPos >= maxSrcFrame)
                {
                    int remainingFrames = framesToOutput - i;
                    Array.Clear(buffer, offset + i * channels, remainingFrames * channels);
                    return framesToOutput * channels;
                }

                int   i0   = (int)srcPos;
                float frac = srcPos - i0;

                int i1 = i0 + 1;
                if (i1 >= framesAvail) i1 = framesAvail - 1;

                int base0   = i0 * channels;
                int base1   = i1 * channels;
                int outBase = offset + i * channels;

                for (int ch = 0; ch < channels; ch++)
                {
                    float s0 = _tempBuffer[base0 + ch];
                    float s1 = _tempBuffer[base1 + ch];
                    buffer[outBase + ch] = s0 + (s1 - s0) * frac;
                }
            }

            return framesToOutput * channels;
        }

        private void EnsureTempBuffer(int samples)
        {
            if (_tempBuffer.Length < samples)
                _tempBuffer = new float[samples];
        }
        
        private float ComputeDopplerRatio()
        {
            var listener = _getListener?.Invoke() ?? ListenerState.Default;

            float c = SpeedOfSound;
            if (c <= 1e-4f)
                return 1.0f;

            Vector3 forward = listener.Forward;
            if (forward.LengthSquared() < 1e-6f)
                forward = new Vector3(0, 0, -1);
            forward = Vector3.Normalize(forward);

            Vector3 dom = DominantDirection;
            if (dom.LengthSquared() < 1e-6f)
                dom = forward;
            dom = Vector3.Normalize(dom);

            float frontDot = Vector3.Dot(forward, dom);
            int   frontSign;
            if (frontDot > 0.001f)
                frontSign = 1;
            else if (frontDot < -0.001f)
                frontSign = 1;
            else
                frontSign = 1;

            float vForward = Vector3.Dot(listener.Velocity, forward);

            float vEffective = vForward * frontSign * DopplerScale;

            float maxVr = 0.9f * c;
            vEffective  = Math.Clamp(vEffective, -maxVr, maxVr);

            float ratio = (c + vEffective) / c;

            if (!float.IsFinite(ratio))
                ratio = 1.0f;

            return ratio;
        }
    }
}
