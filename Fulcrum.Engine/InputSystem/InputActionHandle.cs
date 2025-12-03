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

namespace Fulcrum.Engine.InputSystem
{
    public readonly struct InputActionHandle : IEquatable<InputActionHandle>
    {
        internal string Id { get; }

        internal InputActionHandle(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(Id);

        public override string ToString() => Id ?? "<null>";

        public bool Equals(InputActionHandle other)
        {
            return string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is InputActionHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id != null ? StringComparer.Ordinal.GetHashCode(Id) : 0;
        }

        public static bool operator ==(InputActionHandle left, InputActionHandle right) => left.Equals(right);
        public static bool operator !=(InputActionHandle left, InputActionHandle right) => !left.Equals(right);
    }
}