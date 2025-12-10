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
using Fulcrum.Common;
using Veldrid.Sdl2;

namespace Fulcrum.Engine.Render.Input
{
    public sealed class MouseLockHelper : IDisposable
    {
        private readonly Sdl2Window _window;
        private bool _locked;

        private int _centerX;
        private int _centerY;

        public bool IsLocked => _locked;

        public MouseLockHelper(Sdl2Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            RecalculateCenter();
        }

        public void RecalculateCenter()
        {
            _centerX = _window.Width  / 2;
            _centerY = _window.Height / 2;
        }

        public void SetLocked(bool locked)
        {
            if (_locked == locked)
                return;

            _locked = locked;

            if (locked)
            {
                RecalculateCenter();

                try { SdlCompat.ShowCursor(0); }               catch { }
                try { SdlCompat.SetRelativeMouseMode(false); } catch { }

                try
                {
                    Sdl2Native.SDL_WarpMouseInWindow(
                        _window.SdlWindowHandle,
                        _centerX,
                        _centerY);
                }
                catch { }

                try { SdlCompat.SetRelativeMouseMode(true); }  catch { }
            }
            else
            {
                try { SdlCompat.SetRelativeMouseMode(false); } catch { }
                try { SdlCompat.ShowCursor(1); }               catch { }
            }
        }

        public void Update()
        {
            if (!_locked)
                return;

            try
            {
                Sdl2Native.SDL_WarpMouseInWindow(
                    _window.SdlWindowHandle,
                    _centerX,
                    _centerY);
            }
            catch
            {
                FulcrumEngine.GlobalLogger.Error("Failed to update mouse lock.");
            }
        }

        public void Dispose()
        {
            if (_locked)
            {
                try { SdlCompat.SetRelativeMouseMode(false); } catch { }
                try { SdlCompat.ShowCursor(1); }               catch { }
                _locked = false;
            }
        }
    }
}
