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
using Fulcrum.Engine.Physics;
using Fulcrum.Engine.GameObjectComponent.Phys;
using ImGuiNET;
using Veldrid;
using BepuPhysics;

namespace Fulcrum.Engine.Debug
{
    public static class PhysicsDebugGUI
    {
        private static bool _visible;
        private static bool _lastComboPressed;

        public static void Upd(RenderApp app)
        {
            var renderer = app.Renderer;

            renderer.OnImGuiRender += r =>
            {
                bool isShift = r.IsKeyDown(Key.LShift) || r.IsKeyDown(Key.RShift);
                bool isF7 = r.IsKeyDown(Key.F7);
                bool combo = isShift && isF7;

                if (combo && !_lastComboPressed)
                    _visible = !_visible;

                _lastComboPressed = combo;

                if (!_visible)
                    return;

                ImGui.SetNextWindowSize(new Vector2(640, 520), ImGuiCond.FirstUseEver);
                if (!ImGui.Begin("Physics Debug", ImGuiWindowFlags.NoCollapse))
                {
                    ImGui.End();
                    return;
                }

                // 概览
                ImGui.TextDisabled("Overview");
                ImGui.Separator();

                bool initialized = PhysicsWorld.Initialized;
                ImGui.Text($"Initialized: {initialized}");

                if (!initialized || PhysicsWorld.Simulation == null)
                {
                    ImGui.Text("PhysicsWorld is not initialized or Simulation is null.");
                    ImGui.End();
                    return;
                }

                var sim = PhysicsWorld.Simulation;
                var scene = Scene.Scene.CurrentScene;

                // 统计刚体组件
                int rbCount = 0;
                int kinematicCount = 0;

                if (scene != null && scene.RootObjects != null)
                {
                    foreach (var root in scene.RootObjects)
                    {
                        foreach (var go in root.Traverse())
                        {
                            if (go.Components == null) continue;
                            foreach (var comp in go.Components)
                            {
                                if (comp is RigidBodyComponent rb)
                                {
                                    rbCount++;
                                    if (rb.IsKinematic)
                                        kinematicCount++;
                                }
                            }
                        }
                    }
                }

                ImGui.Text($"RigidBody Components: {rbCount}");
                ImGui.Text($"Kinematic Bodies: {kinematicCount}");

                ImGui.Spacing();
                ImGui.TextDisabled("Simulation");
                ImGui.Separator();
                ImGui.Text($"BufferPool: {(PhysicsWorld.BufferPool != null ? "OK" : "<null>")}");
                ImGui.Text($"ThreadDispatcher: {(PhysicsWorld.ThreadDispatcher != null ? "Custom" : "null (single-thread)")}");
                
                ImGui.Spacing();
                ImGui.TextDisabled("RigidBodies (preview)");
                ImGui.Separator();

                ImGui.BeginChild("Physics_RigidBodiesRegion", new Vector2(0, 0), ImGuiChildFlags.None);

                ImGui.Columns(6, "physics_rb_columns", true);
                ImGui.Text("GameObject"); ImGui.NextColumn();
                ImGui.Text("Mass"); ImGui.NextColumn();
                ImGui.Text("Kinematic"); ImGui.NextColumn();
                ImGui.Text("Position"); ImGui.NextColumn();
                ImGui.Text("Velocity"); ImGui.NextColumn();
                ImGui.Text("Handle"); ImGui.NextColumn();
                ImGui.Separator();

                int shown = 0;
                const int maxShown = 64;

                if (scene != null && scene.RootObjects != null)
                {
                    foreach (var root in scene.RootObjects)
                    {
                        foreach (var go in root.Traverse())
                        {
                            if (go.Components == null) continue;

                            foreach (var comp in go.Components)
                            {
                                if (comp is not RigidBodyComponent rb) continue;
                                if (!rb.HasBody) continue;

                                var bodyRef = sim.Bodies.GetBodyReference(rb.BodyHandle);
                                var pos = bodyRef.Pose.Position;
                                var vel = bodyRef.Velocity.Linear;

                                string name = go.Name ?? "<unnamed>";
                                string posStr = $"({pos.X:0.00}, {pos.Y:0.00}, {pos.Z:0.00})";
                                string velStr = $"({vel.X:0.00}, {vel.Y:0.00}, {vel.Z:0.00})";

                                ImGui.Text(name);
                                ImGui.NextColumn();

                                ImGui.Text($"{rb.Mass:0.###}");
                                ImGui.NextColumn();

                                ImGui.Text(rb.IsKinematic ? "Yes" : "No");
                                ImGui.NextColumn();

                                ImGui.Text(posStr);
                                ImGui.NextColumn();

                                ImGui.Text(velStr);
                                ImGui.NextColumn();

                                ImGui.Text(rb.BodyHandle.Value.ToString());
                                ImGui.NextColumn();

                                shown++;
                                if (shown >= maxShown)
                                    goto DONE_LIST;
                            }
                        }
                    }
                }

            DONE_LIST:
                ImGui.Columns(1);
                if (rbCount > maxShown)
                {
                    ImGui.TextDisabled($"(Only first {maxShown} bodies shown, total {rbCount})");
                }

                ImGui.EndChild();
                ImGui.End();
            };
        }
    }
}
