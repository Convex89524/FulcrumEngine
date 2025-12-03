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
    public class WeldBindingComponent : Component
    {
        /// <summary>主刚体（被跟随者）</summary>
        public RigidBodyComponent BodyA;

        /// <summary>从刚体（跟随者）</summary>
        public RigidBodyComponent BodyB;

        private Vector3 _localOffsetBInA;
        private Quaternion _localRotBInA;

        protected override void OnEnable()
        {
            base.OnEnable();
            RecalculateLocalOffset();
        }

        private void RecalculateLocalOffset()
        {
            if (BodyA?.Owner?.Transform == null || BodyB?.Owner?.Transform == null)
                return;

            var ta = BodyA.Owner.Transform;
            var tb = BodyB.Owner.Transform;

            var invRotA = Quaternion.Inverse(ta.Rotation);

            _localOffsetBInA = Vector3.Transform(tb.Position - ta.Position, invRotA);
            _localRotBInA = Quaternion.Normalize(invRotA * tb.Rotation);
        }

        protected override void FixedUpdate()
        {
            if (!PhysicsWorld.Initialized) return;
            if (BodyA == null || BodyB == null) return;
            if (!BodyA.HasBody || !BodyB.HasBody) return;

            var sim = PhysicsWorld.Simulation;

            var bodyRefA = sim.Bodies.GetBodyReference(BodyA.BodyHandle);
            var bodyRefB = sim.Bodies.GetBodyReference(BodyB.BodyHandle);

            ref var poseA = ref bodyRefA.Pose;

            var worldPosB = poseA.Position + Vector3.Transform(_localOffsetBInA, poseA.Orientation);
            var worldRotB = Quaternion.Normalize(poseA.Orientation * _localRotBInA);

            bodyRefB.Pose.Position = worldPosB;
            bodyRefB.Pose.Orientation = worldRotB;

            bodyRefB.Velocity.Linear = bodyRefA.Velocity.Linear;
            bodyRefB.Velocity.Angular = bodyRefA.Velocity.Angular;

            if (BodyB.Owner?.Transform != null)
            {
                BodyB.Owner.Transform.Position = worldPosB;
                BodyB.Owner.Transform.Rotation = worldRotB;
            }
        }
    }
}
