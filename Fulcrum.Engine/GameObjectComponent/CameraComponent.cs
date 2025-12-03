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

using System;
using System.IO;
using System.Numerics;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Scene;

namespace Fulcrum.Engine.GameObjectComponent
{
    public class CameraComponent : Component, ISceneSerializableComponent
    {
        /// <summary>是否作为主摄像机</summary>
        public bool IsMainCamera { get; set; } = true;

        /// <summary>垂直视角（弧度）</summary>
        public float FieldOfView { get; set; } = MathF.PI / 3f;

        /// <summary>近裁剪面</summary>
        public float NearPlane { get; set; } = 0.1f;

        /// <summary>远裁剪面</summary>
        public float FarPlane { get; set; } = 1000f;

        /// <summary>运行时使用的 Camera 对象</summary>
        public Camera RuntimeCamera { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            RuntimeCamera = FulcrumEngine.RenderApp.Renderer.Camera;

            SyncRuntimeCameraFromTransform();
            SceneCameraBinder.Register(this);
        }

        protected override void OnDestroy()
        {
            SceneCameraBinder.Unregister(this);
            base.OnDestroy();
        }

        protected override void Update(double dt)
        {
            base.Update(dt);
            SyncRuntimeCameraFromTransform();
        }

        internal void SyncRuntimeCameraFromTransform()
        {
            if (RuntimeCamera == null || Owner == null) return;

            var t = Owner.Transform;

            var pos = t.Position;
            var forward = t.Forward;
            var up = t.Up;

            if (forward.LengthSquared() < 1e-6f)
                forward = Vector3.UnitZ;
            if (up.LengthSquared() < 1e-6f)
                up = Vector3.UnitY;

            RuntimeCamera.Position = pos;
            RuntimeCamera.Target = pos + forward;
            RuntimeCamera.Up = up;

            RuntimeCamera.FieldOfView = FieldOfView;
            RuntimeCamera.NearPlane = NearPlane;
            RuntimeCamera.FarPlane = FarPlane;
        }

        #region ISceneSerializableComponent

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(IsMainCamera);
            writer.Write(FieldOfView);
            writer.Write(NearPlane);
            writer.Write(FarPlane);
        }

        public void Deserialize(BinaryReader reader)
        {
            IsMainCamera = reader.ReadBoolean();
            FieldOfView = reader.ReadSingle();
            NearPlane = reader.ReadSingle();
            FarPlane = reader.ReadSingle();
        }

        #endregion
    }
}