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

namespace Fulcrum.Engine.Sound
{
    // public readonly struct Vector3
    // {
    //     public readonly float X, Y, Z;
    //     public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
    //     public static Vector3 Zero => new(0,0,0);
    //     public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X+b.X, a.Y+b.Y, a.Z+b.Z);
    //     public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X-b.X, a.Y-b.Y, a.Z-b.Z);
    //     public static Vector3 operator *(Vector3 a, float s) => new(a.X*s, a.Y*s, a.Z*s);
    //     public float Length() => MathF.Sqrt(X*X + Y*Y + Z*Z);
    //     public static float Dot(in Vector3 a, in Vector3 b) => a.X*b.X + a.Y*b.Y + a.Z*b.Z;
    //     public static Vector3 Normalize(in Vector3 v)
    //     {
    //         var len = v.Length();
    //         return len > 1e-6f ? new Vector3(v.X/len, v.Y/len, v.Z/len) : Zero;
    //     }
    // }

    public readonly struct ListenerState
    {
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;
        public readonly Vector3 Forward; // 归一化
        public readonly Vector3 Up;      // 归一化
        public ListenerState(Vector3 pos, Vector3 vel, Vector3 fwd, Vector3 up)
        {
            Position = pos; Velocity = vel;
            Forward = Vector3.Normalize(fwd);
            Up = Vector3.Normalize(up);
        }
        public static ListenerState Default => new(Vector3.Zero, Vector3.Zero, new Vector3(0,0,-1), new Vector3(0,1,0));
    }
} 