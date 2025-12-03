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
using BepuPhysics;
using Fulcrum.Engine.Scene;

namespace Fulcrum.Engine.GameObjectComponent.Phys
{
    public class AxisJointComponent : Component
    {
        public RigidBodyComponent BodyA;
        public RigidBodyComponent BodyB;

        /// <summary>铰链轴在 A 的本地坐标</summary>
        public Vector3 AxisLocalOnA { get; set; } = Vector3.UnitY;

        /// <summary>锚点初始位置</summary>
        private Vector3 _anchorWorld;

        protected override void OnEnable()
        {
            base.OnEnable();
            RecalculateAnchor();
        }

        private void RecalculateAnchor()
        {
            if (BodyA?.Owner?.Transform == null || BodyB?.Owner?.Transform == null)
                return;

            var pa = BodyA.Owner.Transform.Position;
            var pb = BodyB.Owner.Transform.Position;
            _anchorWorld = (pa + pb) * 0.5f;
        }

        protected override void FixedUpdate()
        {
            if (!PhysicsWorld.Initialized) return;
            if (BodyA == null || BodyB == null) return;
            if (!BodyA.HasBody || !BodyB.HasBody) return;

            var sim = PhysicsWorld.Simulation;

            var bodyRefA = sim.Bodies.GetBodyReference(BodyA.BodyHandle);
            var bodyRefB = sim.Bodies.GetBodyReference(BodyB.BodyHandle);

            bodyRefA.Pose.Position = _anchorWorld;
            bodyRefB.Pose.Position = _anchorWorld;

            if (BodyA.Owner?.Transform != null)
                BodyA.Owner.Transform.Position = _anchorWorld;
            if (BodyB.Owner?.Transform != null)
                BodyB.Owner.Transform.Position = _anchorWorld;
        }
    }
}
