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
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CMLS.CLogger;
using Fulcrum.Common;

namespace Fulcrum.ConfigSystem
{
    public class ConfigBus
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("ConfigBus");

        private readonly Dictionary<string, ConfigEntryBase> _entries =
            new Dictionary<string, ConfigEntryBase>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, JsonElement> _pendingRawValues =
            new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        private bool _loadedOnce;

        public string Name { get; }

        public string FilePath { get; }

        public ConfigBus(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

            string basePath;
            if (!string.IsNullOrWhiteSpace(Global.GameConfigPath))
            {
                basePath = Global.GameConfigPath!;
            }
            else
            {
                var root = AppContext.BaseDirectory;
                basePath = Path.Combine(root, "config");
            }

            Directory.CreateDirectory(basePath);
            FilePath = Path.Combine(basePath, name + ".json");
        }

        public ConfigEntry<T> Register<T>(string key, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("key cannot be null or whitespace.", nameof(key));

            ValidateSupportedType(typeof(T));

            if (_entries.TryGetValue(key, out var existing))
            {
                if (existing is ConfigEntry<T> typedExisting)
                    return typedExisting;

                throw new InvalidOperationException(
                    $"Config key '{key}' already registered with different type: {existing.ValueType.Name}");
            }

            ConfigEntry<T> entry;

            if (_pendingRawValues.TryGetValue(key, out var raw))
            {
                entry = new ConfigEntry<T>(this, key, defaultValue);

                try
                {
                    object? value = entry.ReadFromJsonElement(raw);
                    entry.SetObjectValue(value);
                    LOGGER.Debug($"ConfigBus '{Name}': apply pending value for key '{key}' from json file.");
                }
                catch (Exception e)
                {
                    LOGGER.Warn(
                        $"ConfigBus '{Name}': failed to apply pending value for key '{key}', use default. Error: {e.Message}");
                }

                _pendingRawValues.Remove(key);
            }
            else
            {
                entry = new ConfigEntry<T>(this, key, defaultValue);
            }

            _entries[key] = entry;
            return entry;
        }

        public bool HasKey(string key) => _entries.ContainsKey(key);

        public bool TryGetEntry(string key, out ConfigEntryBase entry)
        {
            return _entries.TryGetValue(key, out entry!);
        }

        public IReadOnlyDictionary<string, ConfigEntryBase> Entries => _entries;

        public void Load()
        {
            _pendingRawValues.Clear();

            if (!File.Exists(FilePath))
            {
                LOGGER.Info($"ConfigBus '{Name}': file not found, will use defaults and save later. File: {FilePath}");
                ApplyDefaults();
                Save();
                _loadedOnce = true;
                return;
            }

            try
            {
                string jsonText = File.ReadAllText(FilePath);
                using var doc = JsonDocument.Parse(jsonText);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    LOGGER.Warn($"ConfigBus '{Name}': root is not an object, ignore file.");
                    ApplyDefaults();
                    _loadedOnce = true;
                    return;
                }

                var root = doc.RootElement;

                ApplyDefaults();

                foreach (var prop in root.EnumerateObject())
                {
                    string key = prop.Name;

                    if (_entries.TryGetValue(key, out var entry))
                    {
                        try
                        {
                            object? value = entry.ReadFromJsonElement(prop.Value);
                            entry.SetObjectValue(value);
                        }
                        catch (Exception e)
                        {
                            LOGGER.Warn(
                                $"ConfigBus '{Name}': failed to parse key '{key}', use default. Error: {e.Message}");
                        }
                    }
                    else
                    {
                        _pendingRawValues[key] = prop.Value.Clone();
                        LOGGER.Debug(
                            $"ConfigBus '{Name}': cache pending key '{key}' from json file.");
                    }
                }

                _loadedOnce = true;
                LOGGER.Info($"ConfigBus '{Name}' loaded from {FilePath}");
            }
            catch (Exception e)
            {
                LOGGER.Warn($"ConfigBus '{Name}': Load failed, use defaults. Error: {e}");
                ApplyDefaults();
                _loadedOnce = true;
            }
        }

        public void Save()
        {
            try
            {
                var options = new JsonWriterOptions
                {
                    Indented = true
                };

                using var stream = File.Create(FilePath);
                using var writer = new Utf8JsonWriter(stream, options);

                writer.WriteStartObject();

                foreach (var kv in _entries)
                {
                    writer.WritePropertyName(kv.Key);
                    kv.Value.WriteToJsonWriter(writer);
                }

                writer.WriteEndObject();
                writer.Flush();

                LOGGER.Info($"ConfigBus '{Name}' saved to {FilePath}");
            }
            catch (Exception e)
            {
                LOGGER.Error($"ConfigBus '{Name}': Save failed: {e}");
            }
        }

        internal void EnsureLoaded()
        {
            if (!_loadedOnce)
            {
                Load();
            }
        }

        private void ApplyDefaults()
        {
            foreach (var kv in _entries)
            {
                kv.Value.ResetToDefault();
            }
        }

        internal static void ValidateSupportedType(Type type)
        {
            if (type == typeof(int) ||
                type == typeof(float) ||
                type == typeof(string) ||
                type == typeof(bool) ||
                type == typeof(short) ||
                type == typeof(ushort))
            {
                return;
            }

            throw new NotSupportedException(
                $"Config type '{type.FullName}' is not supported. " +
                "Supported types: int, float, string, bool, short, ushort.");
        }
    }
}
