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

using System.Collections;
using System.Numerics;
using System.Reflection;
using Fulcrum.Engine.App;
using Fulcrum.Engine.InputSystem;
using ImGuiNET;
using Veldrid;

namespace Fulcrum.Engine.Debug;

public static class InputDebugGUI
{
    private static bool _visible;
    private static bool _lastComboPressed;

    public static void Upd(RenderApp app)
    {
        var renderer = app.Renderer;
        var input = app.Input;

        renderer.OnImGuiRender += r =>
        {
            bool isShift = r.IsKeyDown(Key.LShift) || r.IsKeyDown(Key.RShift);
            bool isF6 = r.IsKeyDown(Key.F6);
            bool combo = isShift && isF6;

            if (combo && !_lastComboPressed)
                _visible = !_visible;

            _lastComboPressed = combo;

            if (!_visible)
                return;

            ImGui.SetNextWindowSize(new Vector2(640, 420), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("Input Debug", ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return;
            }

            if (input == null)
            {
                ImGui.Text("InputContext is null.");
                ImGui.End();
                return;
            }

            ImGui.TextDisabled("Bindings");
            ImGui.Separator();

            var field = typeof(InputContext).GetField("_bindings",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                ImGui.Text("Reflection failed: field \"_bindings\" not found.");
                ImGui.End();
                return;
            }

            var dictObj = field.GetValue(input) as IDictionary;
            if (dictObj == null)
            {
                ImGui.Text("Reflection failed: _bindings is not IDictionary.");
                ImGui.End();
                return;
            }

            ImGui.Text($"Total bindings: {dictObj.Count}");
            ImGui.Spacing();

            ImGui.Columns(4, "input_bindings_columns", true);
            ImGui.Text("Action Id"); ImGui.NextColumn();
            ImGui.Text("Key"); ImGui.NextColumn();
            ImGui.Text("Default Key"); ImGui.NextColumn();
            ImGui.Text("State"); ImGui.NextColumn();
            ImGui.Separator();

            foreach (DictionaryEntry kv in dictObj)
            {
                string id = kv.Key as string ?? "<null>";
                object? binding = kv.Value;

                string keyStr = "<unknown>";
                string defaultKeyStr = "<unknown>";
                bool isDown = false;

                if (binding != null)
                {
                    var bType = binding.GetType();

                    var keyProp = bType.GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
                    var defaultKeyProp = bType.GetProperty("DefaultKey", BindingFlags.Public | BindingFlags.Instance);
                    var idProp = bType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);

                    if (idProp != null)
                    {
                        var realId = idProp.GetValue(binding) as string;
                        if (!string.IsNullOrWhiteSpace(realId))
                            id = realId!;
                    }

                    if (keyProp != null)
                    {
                        var v = keyProp.GetValue(binding);
                        if (v is Key k)
                            keyStr = k.ToString();
                    }

                    if (defaultKeyProp != null)
                    {
                        var v = defaultKeyProp.GetValue(binding);
                        if (v is Key dk)
                            defaultKeyStr = dk.ToString();
                    }
                }

                try
                {
                    isDown = input.IsDown(id);
                }
                catch
                {
                }

                ImGui.Text(id);
                ImGui.NextColumn();

                ImGui.Text(keyStr);
                ImGui.NextColumn();

                ImGui.Text(defaultKeyStr);
                ImGui.NextColumn();

                ImGui.Text(isDown ? "Down" : "-");
                ImGui.NextColumn();
            }

            ImGui.Columns(1);
            ImGui.End();
        };
    }
}
