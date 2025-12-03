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
    public sealed class ConfigEntry<T> : ConfigEntryBase
    {
        private T _value;
        private readonly T _defaultValue;

        public override Type ValueType => typeof(T);

        public override object? DefaultObject => _defaultValue;

        public T Value
        {
            get
            {
                Bus.EnsureLoaded();
                return _value;
            }
            set
            {
                ConfigBus.ValidateSupportedType(typeof(T));
                _value = value;
            }
        }

        internal ConfigEntry(ConfigBus bus, string key, T defaultValue)
            : base(bus, key)
        {
            ConfigBus.ValidateSupportedType(typeof(T));
            _defaultValue = defaultValue;
            _value = defaultValue;
        }

        public override void ResetToDefault()
        {
            _value = _defaultValue;
        }

        public override object? GetObjectValue()
        {
            return Value;
        }

        public override void SetObjectValue(object? value)
        {
            if (value == null)
            {
                _value = _defaultValue;
                return;
            }

            if (value is T t)
            {
                _value = t;
                return;
            }

            try
            {
                _value = (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                _value = _defaultValue;
            }
        }

        internal override object? ReadFromJsonElement(JsonElement element)
        {
            Type t = typeof(T);

            if (t == typeof(int))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int i))
                    return i;
            }
            else if (t == typeof(float))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetSingle(out float f))
                    return f;
            }
            else if (t == typeof(string))
            {
                if (element.ValueKind == JsonValueKind.String)
                    return element.GetString();
                return element.ToString();
            }
            else if (t == typeof(bool))
            {
                if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                    return element.GetBoolean();
            }
            else if (t == typeof(short))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt16(out short s))
                    return s;
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int i))
                    return (short)i;
            }
            else if (t == typeof(ushort))
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetUInt16(out ushort us))
                    return us;
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int i))
                    return (ushort)i;
            }
            else
            {
                throw new NotSupportedException($"Unsupported config type: {t.FullName}");
            }

            return _defaultValue;
        }

        internal override void WriteToJsonWriter(Utf8JsonWriter writer)
        {
            object? v = _value;
            Type t = typeof(T);

            if (t == typeof(int))
            {
                writer.WriteNumberValue((int)(object)v!);
            }
            else if (t == typeof(float))
            {
                writer.WriteNumberValue((float)(object)v!);
            }
            else if (t == typeof(string))
            {
                writer.WriteStringValue((string?)v);
            }
            else if (t == typeof(bool))
            {
                writer.WriteBooleanValue((bool)(object)v!);
            }
            else if (t == typeof(short))
            {
                writer.WriteNumberValue((short)(object)v!);
            }
            else if (t == typeof(ushort))
            {
                writer.WriteNumberValue((ushort)(object)v!);
            }
            else
            {
                throw new NotSupportedException($"Unsupported config type: {t.FullName}");
            }
        }
    }
}
