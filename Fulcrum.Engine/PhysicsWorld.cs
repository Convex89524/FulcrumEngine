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
using BepuPhysics;
using BepuUtilities;
using BepuUtilities.Memory;

namespace Fulcrum.Engine.Scene
{
    public static class PhysicsWorld
    {
        public static Simulation Simulation { get; private set; }

        public static BufferPool BufferPool { get; private set; }

        public static IThreadDispatcher ThreadDispatcher { get; private set; }

        public static bool Initialized => Simulation != null;

        public static void Attach(Simulation simulation, BufferPool bufferPool, IThreadDispatcher dispatcher = null)
        {
            Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            BufferPool = bufferPool ?? throw new ArgumentNullException(nameof(bufferPool));
            ThreadDispatcher = dispatcher;
        }

        public static void StepFixed(double fixedDt)
        {
            if (Simulation == null) return;
            Simulation.Timestep((float)fixedDt, ThreadDispatcher);
        }
    }
}
