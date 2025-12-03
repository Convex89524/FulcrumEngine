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

using System.Collections.Generic;
using System.Numerics;
using Fulcrum.ConfigSystem;
using Fulcrum.Engine.App;
using ImGuiNET;
using Veldrid;

namespace Fulcrum.Engine.Debug;

public static class ConfigDebugGUI
{
    private static bool _visible;
    private static bool _lastComboPressed;

    public static void Upd(RenderApp app)
    {
        var renderer = app.Renderer;

        renderer.OnImGuiRender += r =>
        {
            bool isShift = r.IsKeyDown(Key.LShift) || r.IsKeyDown(Key.RShift);
            bool isF5 = r.IsKeyDown(Key.F5);
            bool combo = isShift && isF5;

            if (combo && !_lastComboPressed)
                _visible = !_visible;

            _lastComboPressed = combo;

            if (!_visible)
                return;

            ImGui.SetNextWindowSize(new Vector2(640, 520), ImGuiCond.FirstUseEver);
            if (!ImGui.Begin("Config Debug", ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
                return;
            }

            var buses = ConfigManager.GetAllBuses();
            ImGui.TextDisabled("Overview");
            ImGui.Separator();
            ImGui.Text($"Total Buses: {buses.Count}");

            if (ImGui.Button("Load All"))
                ConfigManager.LoadAll();
            ImGui.SameLine();
            if (ImGui.Button("Save All"))
                ConfigManager.SaveAll();

            ImGui.Spacing();
            ImGui.BeginChild("ConfigBusesRegion", new Vector2(0, 0), ImGuiChildFlags.None);

            foreach (var bus in buses)
            {
                if (bus == null) continue;

                var entries = bus.Entries ?? (IReadOnlyDictionary<string, ConfigEntryBase>)new Dictionary<string, ConfigEntryBase>();
                string header = $"{bus.Name} ({entries.Count} entries)###bus_{bus.Name}";

                if (ImGui.CollapsingHeader(header, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushID(bus.Name);

                    ImGui.Columns(4, $"bus_columns_{bus.Name}", true);
                    ImGui.Text("Key"); ImGui.NextColumn();
                    ImGui.Text("Type"); ImGui.NextColumn();
                    ImGui.Text("Value"); ImGui.NextColumn();
                    ImGui.Text("Ops"); ImGui.NextColumn();
                    ImGui.Separator();

                    foreach (var kv in entries)
                    {
                        string key = kv.Key;
                        var entry = kv.Value;
                        if (entry == null) continue;

                        object? valueObj = null;
                        try { valueObj = entry.GetObjectValue(); } catch { /* ignore */ }

                        string typeName = entry.ValueType?.Name ?? "<null>";
                        string valueStr = valueObj?.ToString() ?? "<null>";

                        ImGui.Text(key);
                        ImGui.NextColumn();

                        ImGui.Text(typeName);
                        ImGui.NextColumn();

                        ImGui.Text(valueStr);
                        ImGui.NextColumn();

                        if (ImGui.SmallButton($"Reset##{key}"))
                        {
                            entry.ResetToDefault();
                        }

                        ImGui.NextColumn();
                    }

                    ImGui.Columns(1);
                    ImGui.PopID();
                }
            }

            ImGui.EndChild();
            ImGui.End();
        };
    }
}
