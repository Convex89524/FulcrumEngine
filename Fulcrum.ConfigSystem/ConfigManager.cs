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

using CMLS.CLogger;

namespace Fulcrum.ConfigSystem
{
    public static class ConfigManager
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("ConfigSystem");

        private static readonly Dictionary<string, ConfigBus> Buses =
            new Dictionary<string, ConfigBus>(StringComparer.OrdinalIgnoreCase);

        public static ConfigBus GetOrCreateBus(string busName)
        {
            if (string.IsNullOrWhiteSpace(busName))
                throw new ArgumentException("busName cannot be null or whitespace.", nameof(busName));

            if (Buses.TryGetValue(busName, out var existing))
                return existing;

            var bus = new ConfigBus(busName);
            Buses[busName] = bus;
            LOGGER.Info($"ConfigBus created: {busName}, File: {bus.FilePath}");
            return bus;
        }

        public static bool TryGetBus(string busName, out ConfigBus bus)
        {
            return Buses.TryGetValue(busName, out bus!);
        }

        public static IReadOnlyCollection<ConfigBus> GetAllBuses()
        {
            return Buses.Values;
        }

        public static void LoadAll()
        {
            foreach (var bus in Buses.Values)
            {
                try
                {
                    bus.Load();
                }
                catch (Exception e)
                {
                    LOGGER.Warn($"LoadAll: bus '{bus.Name}' load failed: {e}");
                }
            }
        }

        public static void SaveAll()
        {
            foreach (var bus in Buses.Values)
            {
                try
                {
                    bus.Save();
                }
                catch (Exception e)
                {
                    LOGGER.Warn($"SaveAll: bus '{bus.Name}' save failed: {e}");
                }
            }
        }
    }
}
