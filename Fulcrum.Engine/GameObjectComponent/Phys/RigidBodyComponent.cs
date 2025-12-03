// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License
// as published by the Free Software Foundation, version 3 (GPLv3 only).
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
using BepuPhysics.Collidables;
using Fulcrum.Engine.Scene;

namespace Fulcrum.Engine.GameObjectComponent.Phys
{
    public class RigidBodyComponent : Component
    {
        /// <summary>质量</summary>
        public float Mass { get; set; } = 1f;

        /// <summary>盒子碰撞体尺寸</summary>
        public Vector3 Size { get; set; } = new Vector3(1f, 1f, 1f);

        /// <summary>是否为运动学刚体</summary>
        public bool IsKinematic { get; set; } = false;

        /// <summary>刚体句柄</summary>
        public BodyHandle BodyHandle { get; private set; }

        /// <summary>是否已成功加入 Simulation</summary>
        public bool HasBody { get; private set; }

        private TypedIndex _shapeIndex;

        private Transform Transform => Owner?.Transform;

        protected override void OnEnable()
        {
            base.OnEnable();
            TryCreateBody();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RemoveBody();
        }

        protected override void OnDestroy()
        {
            RemoveBody();
            base.OnDestroy();
        }

        public void TryCreateBody()
        {
            if (HasBody) return;
            if (!PhysicsWorld.Initialized) return;
            if (Owner == null || Transform == null) return;

            var sim = PhysicsWorld.Simulation;

            var box = new Box(Size.X, Size.Y, Size.Z);
            _shapeIndex = sim.Shapes.Add(box);

            var position = Transform.Position;
            var orientation = Transform.Rotation;

            var pose = new RigidPose(position, orientation);
            var velocity = new BodyVelocity();
            var collidable = new CollidableDescription(_shapeIndex, maximumSpeculativeMargin: 0.1f);
            var activity = new BodyActivityDescription(0.01f);

            BodyDescription desc;

            if (IsKinematic)
            {
                desc = new BodyDescription
                {
                    Pose = pose,
                    Velocity = velocity,
                    Collidable = collidable,
                    Activity = activity,
                    LocalInertia = new BodyInertia()
                };
            }
            else
            {
                var inertia = box.ComputeInertia(Mass);
                desc = new BodyDescription
                {
                    Pose = pose,
                    Velocity = velocity,
                    Collidable = collidable,
                    Activity = activity,
                    LocalInertia = inertia
                };
            }

            BodyHandle = sim.Bodies.Add(desc);
            HasBody = true;
        }

        private void RemoveBody()
        {
            if (!HasBody) return;
            if (!PhysicsWorld.Initialized) return;

            var sim = PhysicsWorld.Simulation;
            sim.Bodies.Remove(BodyHandle);

            HasBody = false;
        }

        protected override void FixedUpdate()
        {
            if (!HasBody) return;
            if (!PhysicsWorld.Initialized) return;
            if (Transform == null) return;

            var sim = PhysicsWorld.Simulation;
            var body = sim.Bodies.GetBodyReference(BodyHandle);
            ref var pose = ref body.Pose;

            Transform.Position = pose.Position;
            Transform.Rotation = pose.Orientation;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (!HasBody || !PhysicsWorld.Initialized || Transform == null) return;

            var sim = PhysicsWorld.Simulation;
            var body = sim.Bodies.GetBodyReference(BodyHandle);
            body.Pose.Position = position;
            body.Pose.Orientation = rotation;

            Transform.Position = position;
            Transform.Rotation = rotation;
        }
    }
}
