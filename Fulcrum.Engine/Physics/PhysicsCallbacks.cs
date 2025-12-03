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
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;

namespace Fulcrum.Engine.Physics
{
    public struct FulcrumNarrowPhaseCallbacks : INarrowPhaseCallbacks
    {
        public float DefaultFriction;
        public float DefaultMaxRecoveryVelocity;
        public SpringSettings DefaultSpringSettings;

        private Simulation _simulation;

        public FulcrumNarrowPhaseCallbacks(float friction, float maxRecVel, SpringSettings spring)
        {
            DefaultFriction = friction;
            DefaultMaxRecoveryVelocity = maxRecVel;
            DefaultSpringSettings = spring;
            _simulation = null;
        }

        public void Initialize(Simulation simulation)
        {
            _simulation = simulation;
        }

        public bool AllowContactGeneration(
            int workerIndex,
            CollidableReference a,
            CollidableReference b,
            ref float speculativeMargin)
        {
            if (a.Mobility == CollidableMobility.Static && b.Mobility == CollidableMobility.Static)
                return false;

            return true;
        }

        public bool AllowContactGeneration(
            int workerIndex,
            CollidablePair pair,
            int childIndexA,
            int childIndexB)
        {
            return true;
        }

        public void Dispose()
        {
            _simulation = null;
        }

        public bool ConfigureContactManifold(
            int workerIndex,
            CollidablePair pair,
            int childIndexA,
            int childIndexB,
            ref ConvexContactManifold manifold)
        {
            return true;
        }

        public bool ConfigureContactManifold<TManifold>(
            int workerIndex,
            CollidablePair pair,
            ref TManifold manifold,
            out PairMaterialProperties pairMaterial)
            where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial.FrictionCoefficient      = DefaultFriction;
            pairMaterial.MaximumRecoveryVelocity  = DefaultMaxRecoveryVelocity;
            pairMaterial.SpringSettings           = DefaultSpringSettings;
            return true;
        }
    }

    public struct FulcrumPoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        public Vector3 Gravity;
        public float LinearDamping;
        public float AngularDamping;

        private Vector3Wide _gravityWide;

        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

        public bool AllowSubstepsForUnconstrainedBodies => false;
        public bool IntegrateVelocityForKinematics => false;

        public FulcrumPoseIntegratorCallbacks(Vector3 gravity, float linDamp, float angDamp)
        {
            Gravity = gravity;
            LinearDamping = linDamp;
            AngularDamping = angDamp;
            _gravityWide = default;
        }

        public void Initialize(Simulation simulation)
        {
        }

        public void PrepareForIntegration(float dt)
        {
            Vector3Wide.Broadcast(Gravity * dt, out _gravityWide);
        }

        public void IntegrateVelocity(
            Vector<int> bodyIndices,
            Vector3Wide position,
            QuaternionWide orientation,
            BodyInertiaWide localInertia,
            Vector<int> workerIndex,
            int laneCount,
            Vector<float> dt,
            ref BodyVelocityWide velocity)
        {
            Vector<float> zero = Vector<float>.Zero;

            var isDynamic = Vector.GreaterThan(localInertia.InverseMass, zero);

            Vector3Wide.ConditionalSelect(isDynamic, _gravityWide, default, out var gravityDt);

            velocity.Linear += gravityDt;

            var linFactor = Vector<float>.One * (1f - LinearDamping);
            var angFactor = Vector<float>.One * (1f - AngularDamping);

            velocity.Linear.X *= linFactor;
            velocity.Linear.Y *= linFactor;
            velocity.Linear.Z *= linFactor;

            velocity.Angular.X *= angFactor;
            velocity.Angular.Y *= angFactor;
            velocity.Angular.Z *= angFactor;
        }
    }
}
