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

using System.Runtime.InteropServices;

namespace Fulcrum.Common;

/// <summary>
/// SDL2 兼容封装
/// </summary>
public static class SdlCompat
{
    public enum SDL_bool : int { SDL_FALSE = 0, SDL_TRUE = 1 }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint SDL_GetRelativeMouseState(out int x, out int y);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_ShowCursor(int toggle);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_SetRelativeMouseMode(SDL_bool enabled);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_PumpEvents();
        
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_WarpMouseInWindow(IntPtr window, int x, int y);
    
    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint SDL_GetMouseState(out int x, out int y);

    public static void WarpMouseToCenter(IntPtr window, int width, int height)
    {
        SDL_WarpMouseInWindow(window, width / 2, height / 2);
    }

    public static uint GetRelativeMouseState(out int dx, out int dy) => SDL_GetRelativeMouseState(out dx, out dy);
    public static void ShowCursor(int toggle) => SDL_ShowCursor(toggle);
    public static void SetRelativeMouseMode(bool enabled) => SDL_SetRelativeMouseMode(enabled ? SDL_bool.SDL_TRUE : SDL_bool.SDL_FALSE);
    public static void PumpEvents() => SDL_PumpEvents();
}