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

namespace Fulcrum.Engine.Scene
{
    public class Transform
    {
        // —— 基础存储（本地空间）——
        private Vector3 _localPosition = Vector3.Zero;
        private Quaternion _localRotation = Quaternion.Identity;
        private Vector3 _localScale = Vector3.One;

        // —— 关联对象（用于取父节点）——
        public GameObject Owner { get; internal set; }

        // —— 矩阵缓存与脏标记 —— 
        private bool _dirtyLocal = true;
        private bool _dirtyWorld = true;
        private Matrix4x4 _localMatrix = Matrix4x4.Identity;
        private Matrix4x4 _worldMatrix = Matrix4x4.Identity;

        /// <summary>父节点 Transform</summary>
        public Transform Parent => Owner?.Parent?.Transform;

        #region 本地属性（源数据）
        public Vector3 LocalPosition
        {
            get => _localPosition;
            set { _localPosition = value; SetDirty(); }
        }

        /// <summary>本地旋转（四元数）</summary>
        public Quaternion LocalRotation
        {
            get => _localRotation;
            set { _localRotation = NormalizeSafe(value); SetDirty(); }
        }

        /// <summary>本地欧拉角（度）</summary>
        public Vector3 LocalEulerAngles
        {
            get => ToEulerDegrees(_localRotation);
            set { _localRotation = FromEulerDegrees(value); SetDirty(); }
        }

        public Vector3 LocalScale
        {
            get => _localScale;
            set { _localScale = value; SetDirty(); }
        }
        #endregion

        #region 世界属性（便捷）
        /// <summary>世界矩阵（Local 累乘父节点）</summary>
        public Matrix4x4 LocalToWorldMatrix
        {
            get
            {
                RebuildIfDirty();
                return _worldMatrix;
            }
        }

        public Matrix4x4 WorldToLocalMatrix
        {
            get
            {
                var m = LocalToWorldMatrix;
                Matrix4x4.Invert(m, out var inv);
                return inv;
            }
        }

        /// <summary>世界位置</summary>
        public Vector3 Position
        {
            get
            {
                RebuildIfDirty();
                return _worldMatrix.Translation;
            }
            set
            {
                if (Parent != null)
                {
                    var parentInv = Parent.WorldToLocalMatrix;
                    _localPosition = Vector3.Transform(value, parentInv);
                }
                else
                {
                    _localPosition = value;
                }
                SetDirty();
            }
        }

        /// <summary>世界旋转（四元数）</summary>
        public Quaternion Rotation
        {
            get
            {
                var r = _localRotation;
                var p = Parent;
                while (p != null)
                {
                    r = p._localRotation * r;
                    p = p.Parent;
                }
                return NormalizeSafe(r);
            }
            set
            {
                if (Parent != null)
                {
                    var parentWorldRot = Parent.Rotation;
                    _localRotation = NormalizeSafe(Quaternion.Inverse(parentWorldRot) * value);
                }
                else
                {
                    _localRotation = NormalizeSafe(value);
                }
                SetDirty();
            }
        }

        /// <summary>世界欧拉角（度）</summary>
        public Vector3 EulerAngles
        {
            get => ToEulerDegrees(Rotation);
            set => Rotation = FromEulerDegrees(value);
        }

        /// <summary>世界缩放（LossyScale，父子相乘的分量近似）</summary>
        public Vector3 LossyScale
        {
            get
            {
                RebuildIfDirty();
                Matrix4x4.Decompose(_worldMatrix, out var s, out _, out _);
                return s;
            }
        }
        #endregion

        #region 方向向量（世界空间）
        public Vector3 Right => Vector3.Transform(Vector3.UnitX, Matrix3x3(LocalToWorldMatrix));
        public Vector3 Up => Vector3.Transform(Vector3.UnitY, Matrix3x3(LocalToWorldMatrix));
        public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Matrix3x3(LocalToWorldMatrix));
        #endregion

        #region 行为方法
        public enum Space { Self, World }

        /// <summary>按空间平移</summary>
        public void Translate(Vector3 delta, Space space = Space.Self)
        {
            if (space == Space.Self)
            {
                var worldDelta = Vector3.Transform(delta, Matrix3x3(LocalToWorldMatrix));
                Position += worldDelta;
            }
            else
            {
                Position += delta;
            }
        }

        /// <summary>按欧拉角（度）旋转</summary>
        public void Rotate(Vector3 eulerDeltaDeg, Space space = Space.Self)
        {
            var dq = FromEulerDegrees(eulerDeltaDeg);
            if (space == Space.Self)
                LocalRotation = NormalizeSafe(_localRotation * dq);
            else
                Rotation = NormalizeSafe(dq * Rotation);
        }

        /// <summary>朝向目标（世界空间）</summary>
        public void LookAt(Vector3 target, Vector3 worldUp)
        {
            var pos = Position;
            var f = Vector3.Normalize(target - pos);
            if (f.LengthSquared() < 1e-8f) return;

            var r = Vector3.Normalize(Vector3.Cross(worldUp, f));
            if (r.LengthSquared() < 1e-8f)
            {
                worldUp = MathF.Abs(Vector3.Dot(worldUp, Vector3.UnitZ)) > 0.999f ? Vector3.UnitY : Vector3.UnitZ;
                r = Vector3.Normalize(Vector3.Cross(worldUp, f));
            }
            var u = Vector3.Cross(f, r);

            var rotM = new Matrix4x4(
                r.X, r.Y, r.Z, 0,
                u.X, u.Y, u.Z, 0,
                f.X, f.Y, f.Z, 0,
                0,   0,   0,   1);

            Rotation = Quaternion.CreateFromRotationMatrix(rotM);
        }

        /// <summary>设置本地 TRS 一次性操作</summary>
        public void SetLocalTRS(Vector3 pos, Quaternion rot, Vector3 scl)
        {
            _localPosition = pos;
            _localRotation = NormalizeSafe(rot);
            _localScale = scl;
            SetDirty();
        }

        public (Vector3 pos, Quaternion rot, Vector3 scl) GetLocalTRS()
            => (_localPosition, _localRotation, _localScale);

        /// <summary>重置为单位变换</summary>
        public void Reset()
        {
            _localPosition = Vector3.Zero;
            _localRotation = Quaternion.Identity;
            _localScale = Vector3.One;
            SetDirty();
        }

        /// <summary>空间变换：将“局部点”变换到“世界点”</summary>
        public Vector3 TransformPoint(Vector3 localPoint)
            => Vector3.Transform(localPoint, LocalToWorldMatrix);

        /// <summary>空间变换：将“世界点”变换到“局部点”</summary>
        public Vector3 InverseTransformPoint(Vector3 worldPoint)
            => Vector3.Transform(worldPoint, WorldToLocalMatrix);

        /// <summary>空间变换：向量（不含平移）局部→世界</summary>
        public Vector3 TransformVector(Vector3 localVector)
            => Vector3.TransformNormal(localVector, LocalToWorldMatrix);

        /// <summary>空间变换：向量（不含平移）世界→局部</summary>
        public Vector3 InverseTransformVector(Vector3 worldVector)
            => Vector3.TransformNormal(worldVector, WorldToLocalMatrix);

        /// <summary>设置父节点 </summary>
        public void SetParent(GameObject newParent, bool worldPositionStays = true)
        {
            if (Owner == null || newParent == null) return;

            var worldBefore = LocalToWorldMatrix;

            newParent.AddChild(Owner);

            if (worldPositionStays)
            {
                var parentWorld = Parent != null ? Parent.LocalToWorldMatrix : Matrix4x4.Identity;
                Matrix4x4.Invert(parentWorld, out var pInv);
                var newLocal = worldBefore * pInv;

                Matrix4x4.Decompose(newLocal, out var s, out var r, out var t);
                _localPosition = t;
                _localRotation = NormalizeSafe(r);
                _localScale = s;
                SetDirty();
            }
        }
        #endregion

        #region 内部：矩阵重建与工具
        private void SetDirty()
        {
            _dirtyLocal = true;
            _dirtyWorld = true;

            if (Owner == null) return;
            foreach (var c in Owner.Children)
            {
                c.Transform._dirtyWorld = true;
                c.Transform.PropagateDirtyWorld();
            }
        }

        private void PropagateDirtyWorld()
        {
            foreach (var c in Owner.Children)
            {
                c.Transform._dirtyWorld = true;
                c.Transform.PropagateDirtyWorld();
            }
        }

        private void RebuildIfDirty()
        {
            if (_dirtyLocal)
            {
                _localMatrix =
                    Matrix4x4.CreateScale(_localScale) *
                    Matrix4x4.CreateFromQuaternion(_localRotation) *
                    Matrix4x4.CreateTranslation(_localPosition);
                _dirtyLocal = false;
            }

            if (_dirtyWorld)
            {
                if (Parent != null)
                {
                    Parent.RebuildIfDirty();
                    _worldMatrix = _localMatrix * Parent._worldMatrix;
                }
                else
                {
                    _worldMatrix = _localMatrix;
                }
                _dirtyWorld = false;
            }
        }

        private static Quaternion NormalizeSafe(Quaternion q)
        {
            var len = q.Length();
            if (len < 1e-8f) return Quaternion.Identity;
            return Quaternion.Normalize(q);
        }

        private static Vector3 ToEulerDegrees(Quaternion q)
        {
            var m = Matrix4x4.CreateFromQuaternion(q);

            float pitch;
            var sy = -m.M32;
            if (MathF.Abs(sy) >= 0.999999f)
            {
                pitch = MathF.Asin(Math.Clamp(sy, -1f, 1f));
                var yaw = MathF.Atan2(-m.M13, m.M11);
                var roll = 0f;
                return new Vector3(Rad2Deg(pitch), Rad2Deg(yaw), Rad2Deg(roll));
            }
            else
            {
                pitch = MathF.Asin(sy);
                var yaw = MathF.Atan2(m.M31, m.M33);
                var roll = MathF.Atan2(m.M12, m.M22);
                return new Vector3(Rad2Deg(pitch), Rad2Deg(yaw), Rad2Deg(roll));
            }
        }

        private static Quaternion FromEulerDegrees(Vector3 eulerDeg)
        {
            var x = Deg2Rad(eulerDeg.X);
            var y = Deg2Rad(eulerDeg.Y);
            var z = Deg2Rad(eulerDeg.Z);
            var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, y);
            var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, x);
            var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, z);
            return NormalizeSafe(qy * qx * qz);
        }

        private static float Deg2Rad(float d) => d * (MathF.PI / 180f);
        private static float Rad2Deg(float r) => r * (180f / MathF.PI);

        private static Matrix4x4 Matrix3x3(in Matrix4x4 m)
        {
            return new Matrix4x4(
                m.M11, m.M12, m.M13, 0,
                m.M21, m.M22, m.M23, 0,
                m.M31, m.M32, m.M33, 0,
                0,     0,     0,     1);
        }
        #endregion
    }
}
