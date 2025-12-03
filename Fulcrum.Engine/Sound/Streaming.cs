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
using System.Buffers;
using NAudio.Wave;

namespace Fulcrum.Engine.Sound
{
    /// <summary>
    /// 回调式流媒体源
    /// </summary>
    public sealed class CallbackStreamProvider : ISampleProvider
    {
        public delegate int FillCallback(Span<float> dst);

        private readonly WaveFormat _format;
        private readonly FillCallback _onFill;

        public CallbackStreamProvider(WaveFormat format, FillCallback onFill)
        {
            _format = format;
            _onFill = onFill;
        }

        public WaveFormat WaveFormat => _format;

        public int Read(float[] buffer, int offset, int count)
        {
            var span = buffer.AsSpan(offset, count);
            int produced = _onFill(span);
            if (produced < span.Length)
                span.Slice(produced).Clear();
            return produced;
        }
    }
}