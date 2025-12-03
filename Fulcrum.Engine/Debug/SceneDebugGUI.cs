// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License
// as published by
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
using Fulcrum.Engine.Scene;
using ImGuiNET;
using Veldrid;

namespace Fulcrum.Engine.Debug;

public static class SceneDebugGUI
{
    private static bool _visible;
    private static bool _lastComboPressed;

    public static void Upd(RenderApp app)
    {
        var renderer = app.Renderer;

        renderer.OnImGuiRender += r =>
        {
            bool isShift = r.IsKeyDown(Key.LShift) || r.IsKeyDown(Key.RShift);
            bool isF2 = r.IsKeyDown(Key.F2);
            bool combo = isShift && isF2;

            if (combo && !_lastComboPressed)
                _visible = !_visible;

            _lastComboPressed = combo;

            if (!_visible)
                return;

            ImGui.SetNextWindowSize(new Vector2(540, 520), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("Scene Debug", ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return;
            }

            var scene = Scene.Scene.CurrentScene;
            if (scene == null)
            {
                ImGui.Text("CurrentScene is null.");
                ImGui.End();
                return;
            }

            // 概览
            ImGui.TextDisabled("Overview");
            ImGui.Separator();

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

            ImGui.Text($"Root Objects: {rootCount}");
            ImGui.Text($"Total GameObjects: {totalObjects}");
            ImGui.Text($"Total Components: {totalComponents}");
            ImGui.Text($"FixedDeltaTime: {scene.FixedDeltaTime:0.0000}s");

            ImGui.Spacing();
            ImGui.TextDisabled("Hierarchy");
            ImGui.Separator();

            if (scene.RootObjects != null)
            {
                foreach (var root in scene.RootObjects)
                {
                    DrawGameObjectNode(root);
                }
            }

            ImGui.End();
        };
    }

    private static void DrawGameObjectNode(GameObject go)
    {
        if (go == null) return;

        string label = $"{go.Name} [{go.Tag}] (Layer {go.Layer})";

        bool nodeOpen = ImGui.TreeNode(label);
        if (!nodeOpen)
            return;

        // 基本信息
        ImGui.Text($"ActiveSelf: {go.ActiveSelf}");
        ImGui.Text($"ActiveInHierarchy: {go.ActiveInHierarchy}");

        var pos = go.Transform.Position;
        var euler = go.Transform.EulerAngles;
        var scale = go.Transform.LossyScale;

        ImGui.Text($"Position: ({pos.X:0.00}, {pos.Y:0.00}, {pos.Z:0.00})");
        ImGui.Text($"Rotation(Euler): ({euler.X:0.0}, {euler.Y:0.0}, {euler.Z:0.0})");
        ImGui.Text($"Scale: ({scale.X:0.00}, {scale.Y:0.00}, {scale.Z:0.00})");

        ImGui.Spacing();
        ImGui.TextDisabled("Components");
        ImGui.Separator();

        if (go.Components != null && go.Components.Count > 0)
        {
            foreach (var comp in go.Components)
            {
                if (comp == null) continue;
                var t = comp.GetType();
                ImGui.BulletText(t.Name);
            }
        }
        else
        {
            ImGui.Text("<no components>");
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Children");
        ImGui.Separator();

        if (go.Children != null && go.Children.Count > 0)
        {
            foreach (var child in go.Children)
            {
                DrawGameObjectNode(child);
            }
        }
        else
        {
            ImGui.Text("<no children>");
        }

        ImGui.TreePop();
    }
}
