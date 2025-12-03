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
using System.Numerics;

namespace Fulcrum.Engine.Render
{
    // 相机类
    public class Camera
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Target { get; set; } = Vector3.UnitZ;
        public Vector3 Up { get; set; } = Vector3.UnitY;
        public float FieldOfView { get; set; } = MathF.PI / 3f; // 60度
        public float AspectRatio { get; set; } = 16f / 9f;
        public float NearPlane { get; set; } = 0.1f;
        public float FarPlane { get; set; } = 1000f;

        // 添加移动速度属性
        public float MoveSpeed { get; set; } = 5.0f;
        public float RotationSpeed { get; set; } = 2.0f;

        private Vector3 _velocity;
        private float _yaw;
        private float _pitch;

        // 计算前向向量
        public Vector3 Forward => Vector3.Normalize(Target - Position);

        // 计算右向向量
        public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Up));

        // 计算上向向量
        public Vector3 UpVector => Vector3.Normalize(Up);

        public Matrix4x4 GetViewMatrix()
        {
            return Matrix4x4.CreateLookAt(Position, Target, Up);
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
        }

        // 移动相机方法
        public void Move(Vector3 direction, float deltaTime)
        {
            Position += direction * MoveSpeed * deltaTime;
            Target += direction * MoveSpeed * deltaTime;
        }

        // 旋转相机方法
        public void Rotate(float yaw, float pitch, float deltaTime)
        {
            // 计算旋转矩阵
            Matrix4x4 yawRotation = Matrix4x4.CreateFromAxisAngle(UpVector, yaw * RotationSpeed * deltaTime);
            Matrix4x4 pitchRotation = Matrix4x4.CreateFromAxisAngle(Right, pitch * RotationSpeed * deltaTime);

            // 应用旋转到前向向量
            Vector3 newForward = Vector3.Transform(Forward, pitchRotation * yawRotation);

            // 更新目标点
            Target = Position + newForward;
        }

        /// <summary>
        /// 更新相机状态
        /// </summary>
        public void Update(float deltaTime)
        {
            // 应用速度移动相机
            if (_velocity != Vector3.Zero)
            {
                Position += _velocity * deltaTime;
                Target += _velocity * deltaTime;
                _velocity = Vector3.Zero; // 重置速度
            }
        }

        /// <summary>
        /// 设置相机移动速度
        /// </summary>
        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
        }

        /// <summary>
        /// 获取相机的偏航角（Yaw）
        /// </summary>
        public float GetYaw()
        {
            return _yaw;
        }

        /// <summary>
        /// 获取相机的俯仰角（Pitch）
        /// </summary>
        public float GetPitch()
        {
            return _pitch;
        }
        
        /// <summary>
        /// 设置相机的绝对位置
        /// </summary>
        /// <param name="newPosition">新的相机位置</param>
        /// <param name="preserveTargetDirection">是否保持与目标点的相对方向</param>
        public void SetPosition(Vector3 newPosition, bool preserveTargetDirection = true)
        {
            if (preserveTargetDirection)
            {
                Vector3 direction = Target - Position;
                Position = newPosition;
                Target = newPosition + direction;
            }
            else
            {
                Position = newPosition;
            }
        }
        
        /// <summary>
        /// 设置相机的偏航角和俯仰角
        /// </summary>
        public void SetRotation(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = Math.Clamp(pitch, -MathF.PI / 2.0f + 0.1f, MathF.PI / 2.0f - 0.1f);

            // 根据偏航角和俯仰角计算前向向量
            Vector3 front;
            front.X = MathF.Cos(_yaw) * MathF.Cos(_pitch);
            front.Y = MathF.Sin(_pitch);
            front.Z = MathF.Sin(_yaw) * MathF.Cos(_pitch);

            Target = Position + Vector3.Normalize(front);
        }
    }
}