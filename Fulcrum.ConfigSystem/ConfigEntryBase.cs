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

using System;
using System.Text.Json;

namespace Fulcrum.ConfigSystem
{
    public abstract class ConfigEntryBase
    {
        protected readonly ConfigBus Bus;

        public string Key { get; }

        public abstract Type ValueType { get; }

        public abstract object? DefaultObject { get; }

        protected ConfigEntryBase(ConfigBus bus, string key)
        {
            Bus = bus ?? throw new ArgumentNullException(nameof(bus));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public abstract void ResetToDefault();

        public abstract object? GetObjectValue();

        public abstract void SetObjectValue(object? value);

        internal abstract object? ReadFromJsonElement(JsonElement element);

        internal abstract void WriteToJsonWriter(Utf8JsonWriter writer);
    }
}
