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
using BepuPhysics.Constraints;
using Fulcrum.Engine.Scene;

namespace Fulcrum.Engine.GameObjectComponent.Phys
{
    public class SpringJointComponent : Component
    {
        public RigidBodyComponent BodyA;
        public RigidBodyComponent BodyB;

        /// <summary>自然长度</summary>
        public float RestLength { get; set; } = -1f;

        /// <summary>弹簧刚度 k</summary>
        public float Stiffness { get; set; } = 50f;

        /// <summary>阻尼系数 d</summary>
        public float Damping { get; set; } = 5f;

        private bool _initialized;

        protected override void OnEnable()
        {
            base.OnEnable();
            InitRestLengthIfNeeded();
        }

        private void InitRestLengthIfNeeded()
        {
            if (BodyA?.Owner?.Transform == null || BodyB?.Owner?.Transform == null)
                return;

            if (RestLength <= 0f)
            {
                var pa = BodyA.Owner.Transform.Position;
                var pb = BodyB.Owner.Transform.Position;
                RestLength = (pb - pa).Length();
            }

            _initialized = true;
        }

        protected override void FixedUpdate()
        {
            if (!_initialized) InitRestLengthIfNeeded();
            if (!_initialized) return;

            if (!PhysicsWorld.Initialized) return;
            if (BodyA == null || BodyB == null) return;
            if (!BodyA.HasBody || !BodyB.HasBody) return;

            var sim = PhysicsWorld.Simulation;

            var bodyRefA = sim.Bodies.GetBodyReference(BodyA.BodyHandle);
            var bodyRefB = sim.Bodies.GetBodyReference(BodyB.BodyHandle);

            var pa = bodyRefA.Pose.Position;
            var pb = bodyRefB.Pose.Position;

            var dir = pb - pa;
            var dist = dir.Length();
            if (dist < 1e-5f) return;

            var n = dir / dist;

            var x = dist - RestLength;

            var va = bodyRefA.Velocity.Linear;
            var vb = bodyRefB.Velocity.Linear;
            var relVel = Vector3.Dot(vb - va, n);

            var forceScalar = -Stiffness * x - Damping * relVel;
            var force = n * forceScalar;

            var scene = Scene.Scene.CurrentScene;
            var dt = scene?.FixedDeltaTime ?? (1.0 / 50.0);
            var impulse = force * (float)dt;

            bodyRefA.ApplyLinearImpulse(-impulse);
            bodyRefB.ApplyLinearImpulse(impulse);
        }
    }
}
