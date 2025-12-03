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

namespace Fulcrum.Engine.Scene;

public static class EngineEvents
{
    /// <summary>逻辑帧节拍（秒）</summary>
    public static event Action<double> OnLogicTick;

    /// <summary>引擎退出</summary>
    public static event Action OnEngineShutdown;

    /// <summary>Engine 侧调用:触发逻辑帧</summary>
    public static void RaiseLogicTick(double deltaTime)
    {
        OnLogicTick?.Invoke(deltaTime);
    }

    /// <summary>Engine 侧调用:触发引擎退出</summary>
    public static void RaiseEngineShutdown()
    {
        OnEngineShutdown?.Invoke();
    }
}