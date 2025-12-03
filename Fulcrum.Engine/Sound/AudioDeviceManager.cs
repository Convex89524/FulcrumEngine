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
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Fulcrum.Engine.Sound
{
    public sealed class AudioDeviceInfo
    {
        public string Id { get; init; } = "";
        public string FriendlyName { get; init; } = "";
        public int Channels { get; init; }
        public int SampleRate { get; init; }
    }

    /// <summary>
    /// 输出设备选择/查询（基于 WASAPI），可扩展 ASIO/DirectSound
    /// </summary>
    public sealed class AudioDeviceManager : IDisposable
    {
        private readonly ILogger _log;
        private readonly MMDeviceEnumerator _enum = new();
        private MMDevice? _current;

        public AudioDeviceManager(ILogger log) { _log = log; }

        public IReadOnlyList<AudioDeviceInfo> ListWasapiRenderDevices()
        {
            var list = new List<AudioDeviceInfo>();
            foreach (var dev in _enum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                var mix = dev.AudioClient.MixFormat;
                list.Add(new AudioDeviceInfo {
                    Id = dev.ID,
                    FriendlyName = dev.FriendlyName,
                    Channels = mix.Channels,
                    SampleRate = mix.SampleRate
                });
            }
            return list;
        }

        public MMDevice GetDefaultRenderDevice()
        {
            return _enum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }

        public MMDevice OpenByIdOrDefault(string? id)
        {
            _current = !string.IsNullOrWhiteSpace(id) ? _enum.GetDevice(id) : GetDefaultRenderDevice();
            _log.Info($"Using render device: {_current.FriendlyName}");
            return _current;
        }

        public MMDevice Current => _current ?? GetDefaultRenderDevice();

        public void Dispose()
        {
            _current?.Dispose();
            _enum.Dispose();
        }
    }
}