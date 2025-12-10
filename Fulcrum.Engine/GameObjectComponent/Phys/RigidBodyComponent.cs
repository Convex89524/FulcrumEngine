using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Fulcrum.Engine.Scene;
using System.Collections.Generic;

namespace Fulcrum.Engine.GameObjectComponent.Phys
{
    public enum ColliderShapeType
    {
        Box,
        Sphere,
        Cylinder,
        TriangularPrism,
        Wedge
    }

    public class RigidBodyComponent : Component
    {
        private static readonly List<RigidBodyComponent> _activeBodies = new();

        // 新增：从 BodyHandle 映射回 RigidBodyComponent，用于物理回调里查找刚体弹性
        private static readonly Dictionary<BodyHandle, RigidBodyComponent> _handleMap = new();

        internal static bool TryGetByHandle(BodyHandle handle, out RigidBodyComponent rb)
        {
            return _handleMap.TryGetValue(handle, out rb);
        }

        private float _mass = 1f;
        public float Mass
        {
            get => _mass;
            set
            {
                if (value <= 0f) value = 1f;
                if (System.Math.Abs(_mass - value) < 1e-6f) return;
                _mass = value;
                if (HasBody && !_isKinematic)
                    RebuildBody();
            }
        }

        private Vector3 _size = new Vector3(1f, 1f, 1f);
        public Vector3 Size
        {
            get => _size;
            set
            {
                if (_size == value) return;
                _size = value;
                if (HasBody)
                    RebuildBody();
            }
        }

        private bool _isKinematic;
        public bool IsKinematic
        {
            get => _isKinematic;
            set
            {
                if (_isKinematic == value) return;
                _isKinematic = value;
                if (HasBody)
                    RebuildBody();
            }
        }

        private ColliderShapeType _shapeType = ColliderShapeType.Box;
        public ColliderShapeType ShapeType
        {
            get => _shapeType;
            set
            {
                if (_shapeType == value) return;
                _shapeType = value;
                if (HasBody)
                    RebuildBody();
            }
        }

        private float _bounciness = 0.2f;
        public float Bounciness
        {
            get => _bounciness;
            set
            {
                _bounciness = System.MathF.Max(0f, System.MathF.Min(10f, value));
            }
        }

        public BodyHandle BodyHandle { get; private set; }
        public bool HasBody { get; private set; }

        private TypedIndex _shapeIndex;
        private Transform Transform => Owner?.Transform;

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();

            if (Transform != null)
                Transform.Changed += OnTransformChanged;

            TryCreateBody();

            if (!_activeBodies.Contains(this))
                _activeBodies.Add(this);
        }

        protected override void OnDisable()
        {
            if (Transform != null)
                Transform.Changed -= OnTransformChanged;

            base.OnDisable();
            RemoveBody();

            _activeBodies.Remove(this);
        }

        protected override void OnDestroy()
        {
            if (Transform != null)
                Transform.Changed -= OnTransformChanged;

            RemoveBody();
            _activeBodies.Remove(this);
            base.OnDestroy();
        }

        #endregion

        #region 创建 / 销毁 刚体

        private void TryCreateBody()
        {
            if (HasBody) return;
            if (!PhysicsWorld.Initialized) return;
            if (Transform == null) return;

            var sim = PhysicsWorld.Simulation;

            BodyInertia inertia = default;
            CollidableDescription collidable;

            var pose = new RigidPose(Transform.Position, Transform.Rotation);
            var velocity = new BodyVelocity();
            var activity = new BodyActivityDescription(0.01f);

            if (_isKinematic)
            {
                switch (_shapeType)
                {
                    case ColliderShapeType.Sphere:
                    {
                        float diameter = (_size.X + _size.Y + _size.Z) / 3f;
                        float radius = diameter * 0.5f;
                        var sphere = new Sphere(radius);
                        _shapeIndex = sim.Shapes.Add(sphere);
                        if (!_isKinematic)
                            inertia = sphere.ComputeInertia(_mass);
                        break;
                    }
                    case ColliderShapeType.Cylinder:
                    {
                        float radius = System.MathF.Max(_size.X, _size.Z) * 0.5f;
                        float height = _size.Y;
                        var cylinder = new Cylinder(radius, height);
                        _shapeIndex = sim.Shapes.Add(cylinder);
                        if (!_isKinematic)
                            inertia = cylinder.ComputeInertia(_mass);
                        break;
                    }
                    case ColliderShapeType.TriangularPrism:
                    case ColliderShapeType.Wedge:
                    case ColliderShapeType.Box:
                    default:
                    {
                        var box = new Box(_size.X, _size.Y, _size.Z);
                        _shapeIndex = sim.Shapes.Add(box);
                        if (!_isKinematic)
                            inertia = box.ComputeInertia(_mass);
                        break;
                    }
                }

                collidable = new CollidableDescription(_shapeIndex, maximumSpeculativeMargin: 0.1f);

                var desc = new BodyDescription
                {
                    Pose = pose,
                    Velocity = velocity,
                    Collidable = collidable,
                    Activity = activity,
                    LocalInertia = new BodyInertia()
                };

                BodyHandle = sim.Bodies.Add(desc);
            }
            else
            {
                switch (_shapeType)
                {
                    case ColliderShapeType.Sphere:
                    {
                        float diameter = (_size.X + _size.Y + _size.Z) / 3f;
                        float radius = diameter * 0.5f;
                        var sphere = new Sphere(radius);
                        _shapeIndex = sim.Shapes.Add(sphere);
                        inertia = sphere.ComputeInertia(_mass);
                        break;
                    }
                    case ColliderShapeType.Cylinder:
                    {
                        float radius = System.MathF.Max(_size.X, _size.Z) * 0.5f;
                        float height = _size.Y;
                        var cylinder = new Cylinder(radius, height);
                        _shapeIndex = sim.Shapes.Add(cylinder);
                        inertia = cylinder.ComputeInertia(_mass);
                        break;
                    }
                    default:
                    {
                        var box = new Box(_size.X, _size.Y, _size.Z);
                        _shapeIndex = sim.Shapes.Add(box);
                        inertia = box.ComputeInertia(_mass);
                        break;
                    }
                }

                collidable = new CollidableDescription(_shapeIndex, maximumSpeculativeMargin: 0.1f);

                var desc = new BodyDescription
                {
                    Pose = pose,
                    Velocity = velocity,
                    Collidable = collidable,
                    Activity = activity,
                    LocalInertia = inertia
                };

                BodyHandle = sim.Bodies.Add(desc);
            }

            _handleMap[BodyHandle] = this;

            HasBody = true;
        }

        private void RemoveBody()
        {
            if (!HasBody) return;

            if (!PhysicsWorld.Initialized)
            {
                _handleMap.Remove(BodyHandle);
                HasBody = false;
                return;
            }

            var sim = PhysicsWorld.Simulation;

            _handleMap.Remove(BodyHandle);
            sim.Bodies.Remove(BodyHandle);

            HasBody = false;
        }

        private void RebuildBody()
        {
            if (!PhysicsWorld.Initialized) return;
            RemoveBody();
            TryCreateBody();
        }

        #endregion

        #region 同步 Transform

        public void SyncFromPhysics()
        {
            if (!HasBody || !PhysicsWorld.Initialized || Transform == null) return;

            var sim = PhysicsWorld.Simulation;
            var body = sim.Bodies.GetBodyReference(BodyHandle);
            ref var pose = ref body.Pose;

            if (!_isKinematic)
            {
                Transform.BeginExternalUpdate();
                Transform.Position = pose.Position;
                Transform.Rotation = pose.Orientation;
                Transform.EndExternalUpdate();
            }
        }
        
        public static void SyncAllFromPhysics()
        {
            if (!PhysicsWorld.Initialized)
                return;

            foreach (var rb in _activeBodies)
            {
                if (rb != null && rb.Enabled && rb.HasBody)
                {
                    rb.SyncFromPhysics();
                }
            }
        }

        public static void SyncAllToPhysics()
        {
            if (!PhysicsWorld.Initialized)
                return;

            foreach (var rb in _activeBodies)
            {
                if (rb != null && rb.Enabled && rb.HasBody && rb.Transform != null)
                {
                    rb.Teleport(rb.Transform.Position, rb.Transform.Rotation);
                }
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (!HasBody || !PhysicsWorld.Initialized || Transform == null) return;

            var sim = PhysicsWorld.Simulation;
            var body = sim.Bodies.GetBodyReference(BodyHandle);
            body.Pose.Position = position;
            body.Pose.Orientation = rotation;

            Transform.BeginExternalUpdate();
            Transform.Position = position;
            Transform.Rotation = rotation;
            Transform.EndExternalUpdate();
        }
        
        private void OnTransformChanged(Transform t)
        {
            if (!PhysicsWorld.Initialized || !HasBody || Transform == null)
                return;

            var sim = PhysicsWorld.Simulation;
            var body = sim.Bodies.GetBodyReference(BodyHandle);

            body.Pose.Position = t.Position;
            body.Pose.Orientation = t.Rotation;
        }

        #endregion
    }
}
