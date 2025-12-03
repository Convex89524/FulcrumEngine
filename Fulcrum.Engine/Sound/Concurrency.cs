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
using System.Threading;

namespace Fulcrum.Engine.Sound
{
    /// <summary>
    /// 读多写少的轻量锁，保护引擎状态（监听者/总线树/设备切换等）
    /// </summary>
    internal sealed class ReadWriteScope
    {
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
        public IDisposable Read()  => new Guard(_lock, false);
        public IDisposable Write() => new Guard(_lock, true);

        private sealed class Guard : IDisposable
        {
            private readonly ReaderWriterLockSlim _lck;
            private readonly bool _write;
            public Guard(ReaderWriterLockSlim lck, bool write)
            {
                _lck = lck; _write = write;
                if (write) _lck.EnterWriteLock(); else _lck.EnterReadLock();
            }
            public void Dispose()
            {
                if (_write) _lck.ExitWriteLock(); else _lck.ExitReadLock();
            }
        }
    }
}