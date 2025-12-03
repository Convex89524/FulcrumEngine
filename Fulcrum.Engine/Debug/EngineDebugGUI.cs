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
using Fulcrum.ConfigSystem;
using Fulcrum.Engine.App;
using ImGuiNET;
using Veldrid;

namespace Fulcrum.Engine.Debug;

public static class EngineDebugGUI
{
    private static bool _visible;
    private static bool _lastComboPressed;

    public static void Upd(RenderApp app)
    {
        var renderer = app.Renderer;

        renderer.OnImGuiRender += r =>
        {
            bool isShift = r.IsKeyDown(Key.LShift) || r.IsKeyDown(Key.RShift);
            bool isF1 = r.IsKeyDown(Key.F1);
            bool combo = isShift && isF1;

            if (combo && !_lastComboPressed)
                _visible = !_visible;

            _lastComboPressed = combo;

            if (!_visible)
                return;

            ImGui.SetNextWindowSize(new Vector2(460, 380), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("Engine Debug", ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return;
            }

            // 概览
            ImGui.TextDisabled("Runtime");
            ImGui.Separator();
            ImGui.Text($"IsRun: {FulcrumEngine.IsRun}");
            ImGui.Text($"ServerTick: {FulcrumEngine.ServerTick}");

            var startup = FulcrumEngine.StartupOptions;
            ImGui.Spacing();
            ImGui.TextDisabled("Startup Options");
            ImGui.Separator();
            if (startup != null)
            {
                ImGui.Text($"LoadSceneFromFile: {startup.LoadSceneFromFile}");
                ImGui.Text($"SceneFilePath: {startup.SceneFilePath ?? "<null>"}");
            }
            else
            {
                ImGui.Text("StartupOptions: <null>");
            }

            // 场景信息
            ImGui.Spacing();
            ImGui.TextDisabled("Scene");
            ImGui.Separator();
            var scene = Scene.Scene.CurrentScene;
            if (scene == null)
            {
                ImGui.Text("CurrentScene: <null>");
            }
            else
            {
                int rootCount = scene.RootObjects?.Count ?? 0;
                int totalObjects = 0;
                int totalComponents = 0;

                if (scene.RootObjects != null)
                {
                    foreach (var root in scene.RootObjects)
                    {
                        foreach (var go in root.Traverse())
                        {
                            totalObjects++;
                            totalComponents += go.Components?.Count ?? 0;
                        }
                    }
                }

                ImGui.Text($"CurrentScene: {scene}");
                ImGui.Text($"Root Objects: {rootCount}");
                ImGui.Text($"Total GameObjects: {totalObjects}");
                ImGui.Text($"Total Components: {totalComponents}");
                ImGui.Text($"FixedDeltaTime: {scene.FixedDeltaTime:0.0000}s");
            }

            ImGui.Spacing();
            ImGui.TextDisabled("ConfigSystem");
            ImGui.Separator();
            var buses = ConfigManager.GetAllBuses();
            ImGui.Text($"Config Buses: {buses.Count}");

            ImGui.End();
        };
    }
}
