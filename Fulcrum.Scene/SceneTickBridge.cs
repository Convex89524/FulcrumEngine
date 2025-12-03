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

namespace Fulcrum.Engine.Scene
{
    public static class SceneTickBridge
    {
        private static bool _hooked;

        static SceneTickBridge()
        {
            Hook();
        }

        public static void Hook()
        {
            if (_hooked) return;
            EngineEvents.OnLogicTick += HandleTick;
            EngineEvents.OnEngineShutdown += HandleShutdown;
            _hooked = true;
        }

        public static void Unhook()
        {
            if (!_hooked) return;
            EngineEvents.OnLogicTick -= HandleTick;
            EngineEvents.OnEngineShutdown -= HandleShutdown;
            _hooked = false;
        }

        private static void HandleTick(double dt)
        {
            var scene = Scene.CurrentScene;
            scene?.Step(dt);
        }

        public static void HandleOnRenderFrame(double dt)
        {
            var scene = Scene.CurrentScene;
            scene?.OnRenderFrame(dt);
        }

        private static void HandleShutdown()
        {
            Unhook();
        }
    }
}
