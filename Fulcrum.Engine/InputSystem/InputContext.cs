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
using System.Collections.Generic;
using CMLS.CLogger;
using Fulcrum.ConfigSystem;
using Fulcrum.Engine.Render;
using Veldrid;

namespace Fulcrum.Engine.InputSystem
{
    public sealed class InputContext : IDisposable
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("InputContext");

        private readonly RendererBase _renderer;
        private readonly ConfigBus _bus;

        private readonly Dictionary<string, Binding> _bindings =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly object _lock = new();

        private bool _disposed;

        private sealed class Binding
        {
            public string Id { get; }
            public ConfigEntry<string> Entry { get; }
            public Key Key { get; private set; }
            public Key DefaultKey { get; }

            public Binding(string id, ConfigEntry<string> entry, Key defaultKey)
            {
                Id = id;
                Entry = entry;
                DefaultKey = defaultKey;
                Key = defaultKey;
            }

            public void RefreshKey()
            {
                string raw = Entry.Value ?? string.Empty;

                if (string.IsNullOrWhiteSpace(raw))
                {
                    Entry.Value = DefaultKey.ToString();
                    Key = DefaultKey;
                    return;
                }

                if (!Enum.TryParse<Key>(raw, ignoreCase: true, out var parsed))
                {
                    Entry.Value = DefaultKey.ToString();
                    Key = DefaultKey;
                    return;
                }

                Key = parsed;
            }
        }

        public InputContext(RendererBase renderer, string busName = "input")
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _bus = ConfigManager.GetOrCreateBus(busName);

            _bus.Load();
        }

        public InputActionHandle RegisterAction(string id, Key defaultKey)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("id cannot be null or whitespace.", nameof(id));

            id = NormalizeId(id);

            lock (_lock)
            {
                if (_bindings.TryGetValue(id, out var existing))
                {
                    return new InputActionHandle(existing.Id);
                }

                var entry = _bus.Register<string>(id, defaultKey.ToString());
                var binding = new Binding(id, entry, defaultKey);
                binding.RefreshKey();

                _bindings[id] = binding;

                return new InputActionHandle(id);
            }
        }

        public bool IsDown(InputActionHandle handle)
        {
            if (!handle.IsValid || _disposed)
                return false;

            return IsDown(handle.ToString());
        }

        public bool IsDown(string id)
        {
            if (_disposed) return false;
            if (string.IsNullOrWhiteSpace(id)) return false;

            id = NormalizeId(id);

            Binding? binding;
            lock (_lock)
            {
                if (!_bindings.TryGetValue(id, out binding))
                {
                    LOGGER.Warn($"IsDown: action '{id}' not registered.");
                    return false;
                }
            }

            if (binding.Key == Key.Unknown) return false;

            return _renderer.IsKeyDown(binding.Key);
        }

        public Key GetKey(InputActionHandle handle)
        {
            if (!handle.IsValid || _disposed) return Key.Unknown;

            return GetKey(handle.ToString());
        }

        public Key GetKey(string id)
        {
            if (_disposed) return Key.Unknown;
            if (string.IsNullOrWhiteSpace(id)) return Key.Unknown;

            id = NormalizeId(id);

            lock (_lock)
            {
                if (_bindings.TryGetValue(id, out var binding))
                    return binding.Key;
            }

            return Key.Unknown;
        }

        public void Rebind(InputActionHandle handle, Key newKey, bool autoSave = true)
        {
            if (!handle.IsValid || _disposed) return;

            Rebind(handle.ToString(), newKey, autoSave);
        }

        public void Rebind(string id, Key newKey, bool autoSave = true)
        {
            if (_disposed) return;
            if (string.IsNullOrWhiteSpace(id)) return;

            id = NormalizeId(id);

            Binding? binding;
            lock (_lock)
            {
                if (!_bindings.TryGetValue(id, out binding))
                {
                    LOGGER.Warn($"Rebind: action '{id}' not registered.");
                    return;
                }

                string keyName = newKey.ToString();
                binding.Entry.Value = keyName;
                binding.RefreshKey();

                LOGGER.Info($"Action '{id}' rebind to '{binding.Key}'.");
            }

            if (autoSave)
            {
                try
                {
                    ConfigManager.SaveAll();
                }
                catch (Exception e)
                {
                    LOGGER.Warn($"Rebind: SaveAll failed: {e.Message}");
                }
            }
        }

        public void ReloadFromConfig()
        {
            if (_disposed) return;

            _bus.Load();

            lock (_lock)
            {
                foreach (var kv in _bindings)
                {
                    kv.Value.RefreshKey();
                }
            }

            LOGGER.Info("InputContext: ReloadFromConfig completed.");
        }

        private static string NormalizeId(string id)
        {
            return id.Trim();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_lock)
            {
                _bindings.Clear();
            }
        }
    }
}