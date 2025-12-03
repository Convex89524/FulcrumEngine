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

using System.Numerics;
using Fulcrum.Engine.App;
using Fulcrum.Engine.Sound;
using ImGuiNET;
using Veldrid;

namespace Fulcrum.Engine.Debug;

public class AudioDebugGUI
{
    private static bool _visible = false;
    private static bool _lastComboPressed = false; 

    public static void Upd(AudioEngine audioEngine)
    {
        FulcrumEngine.RenderApp.Renderer.OnImGuiRender += r =>
        {
            bool isShift = r.IsKeyDown(Key.LShift) || r.IsKeyDown(Key.RShift);
            bool isF = r.IsKeyDown(Key.F4);
            bool combo = isShift && isF;

            if (combo && !_lastComboPressed)
            {
                _visible = !_visible;
            }
            _lastComboPressed = combo;

            if (!_visible)
                return;

            ImGui.Begin("3D Audio Debug");
        
            var listener = audioEngine.Listener;
            ImGui.Text($"Listener Position: {listener.Position}");
            ImGui.Text($"Listener Forward: {listener.Forward}");
        
            var relativeLeft = FulcrumEngine.RenderApp.Renderer.GetEngineCoordinator().GetSoundRelativePosition("left_speaker");
            var relativeRight = FulcrumEngine.RenderApp.Renderer.GetEngineCoordinator().GetSoundRelativePosition("right_speaker");
            var relativeMoving = FulcrumEngine.RenderApp.Renderer.GetEngineCoordinator().GetSoundRelativePosition("moving_vehicle");
        
            ImGui.Text($"Left Sound Relative: {relativeLeft}");
            ImGui.Text($"Right Sound Relative: {relativeRight}");
            ImGui.Text($"Moving Sound Relative: {relativeMoving}");
        
            ImGui.End();
        };
    }
}