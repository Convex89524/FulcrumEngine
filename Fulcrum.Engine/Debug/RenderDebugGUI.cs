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
using ImGuiNET;
using Veldrid;

namespace Fulcrum.Engine.Debug
{
    public class RenderDebugGUI
    {
        private static bool _visible = false;
        private static bool _lastComboPressed = false;

        public static void Upd(RenderApp app)
        {
            var rApp = app;
            var renderer = app.Renderer;

            renderer.OnImGuiRender += r =>
            {
                bool isShift = r.IsKeyDown(Key.LShift) || r.IsKeyDown(Key.RShift);
                bool isF3 = r.IsKeyDown(Key.F3);
                bool combo = isShift && isF3;

                if (combo && !_lastComboPressed)
                    _visible = !_visible;
                _lastComboPressed = combo;

                if (!_visible) return;

                ImGui.SetNextWindowSize(new Vector2(520, 520), ImGuiCond.FirstUseEver);
                ImGui.Begin("Renderer Debug", ImGuiWindowFlags.NoCollapse);

                ImGui.BeginTabBar("debugtabs");

                //==============================
                // 1) Overview
                //==============================
                if (ImGui.BeginTabItem("Overview"))
                {
                    ImGui.TextDisabled("Performance");
                    ImGui.Separator();
                    ImGui.Text($"FPS (Current): {renderer.GetCurrentFPS():0.0}");
                    ImGui.Text($"FPS (Target): {renderer.GetTargetFPS()}");
                    ImGui.Text($"Delta Time: {renderer.GetDeltaTime():0.000} s");

                    ImGui.Spacing();
                    ImGui.TextDisabled("Camera");
                    ImGui.Separator();
                    ImGui.Text(
                        $"Pos: {renderer.Camera.Position.X:0.00}, {renderer.Camera.Position.Y:0.00}, {renderer.Camera.Position.Z:0.00}");
                    ImGui.Text($"FOV: {renderer.Camera.FieldOfView:0.00}");
                    ImGui.Text($"Near/Far: {renderer.Camera.NearPlane}/{renderer.Camera.FarPlane}");

                    ImGui.EndTabItem();
                }

                //==============================
                // 2) Renderer Info
                //==============================
                if (ImGui.BeginTabItem("Renderer"))
                {
                    ImGui.TextDisabled("System");
                    ImGui.Separator();
                    ImGui.Text($"Backend: {renderer.GraphicsBackendName}");
                    ImGui.Text($"Threads: {renderer.RenderThreadCount}");

                    ImGui.Spacing();
                    ImGui.TextDisabled("Draw Stats");
                    ImGui.Separator();
                    ImGui.Text($"Renderables: {r.GetRenderableCount}");
                    ImGui.Text($"Resources: {r.GetResourceCount}");

                    ImGui.EndTabItem();
                }

                //==============================
                // 3) Controls
                //==============================
                if (ImGui.BeginTabItem("Controls"))
                {
                    int fps = renderer.GetTargetFPS();
                    if (ImGui.SliderInt("Target FPS", ref fps, 1, 540))
                        renderer.SetFPS(fps);

                    int threads = renderer.RenderThreadCount;
                    if (ImGui.SliderInt("Render Threads", ref threads, 1, 8))
                        renderer.SetRenderThreadCount(threads);

                    ImGui.EndTabItem();
                }

                //==============================
                // 4) Slow Debug Mode
                //==============================
                if (ImGui.BeginTabItem("Slow Debug"))
                {
                    bool slowMode = renderer.DebugSlowModeEnabled;
                    int delay = renderer.DebugDelayPerDrawMs;
                    bool stepMode = renderer.DebugStepMode;

                    ImGui.TextDisabled("Debug Mode Controls");
                    ImGui.Separator();

                    if (ImGui.Checkbox("Enable Slow Debug Mode", ref slowMode))
                        renderer.EnableSlowDebugMode(slowMode, delay, stepMode);

                    if (slowMode)
                    {
                        if (ImGui.SliderInt("Delay Per Draw (ms)", ref delay, 0, 2000))
                            renderer.EnableSlowDebugMode(true, delay, stepMode);

                        if (ImGui.Checkbox("Step Mode (Manual Step)", ref stepMode))
                            renderer.EnableSlowDebugMode(true, delay, stepMode);

                        if (stepMode)
                        {
                            if (ImGui.Button("Step Once"))
                                renderer.DebugStepOnce();
                        }

                        ImGui.Spacing();
                        ImGui.TextDisabled("Current Frame Info");
                        ImGui.Separator();
                        ImGui.Text($"Index: {renderer.DebugCurrentIndex}/{renderer.DebugTotalCount}");
                        ImGui.Text($"Renderable: {renderer.DebugCurrentRenderableName}");
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                ImGui.End();
            };
        }
    }
}