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
using System.Numerics;
using Fulcrum.Common;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Fulcrum.Engine.Sound
{
    /// <summary>
    /// 音频引擎: 设备->主混音->子总线->音源
    /// </summary>
    public sealed class AudioEngine : IDisposable
    {
        private readonly ILogger _log;
        private readonly AudioDeviceManager _devMgr;

        private MMDevice _device;
        private IWavePlayer _output;
        private readonly WaveFormat _engineFormat;
        private readonly MixingSampleProvider _masterMixer;
        private readonly Dictionary<string, AudioBus> _buses = new();

        private readonly ReadWriteScope _rw = new();
        private ListenerState _listener = ListenerState.Default;

        public AudioEngine(ILogger? logger = null, int sampleRate = 48000)
        {
            _log = logger ?? new ConsoleLogger("AudioEngine");
            _devMgr = new AudioDeviceManager(_log);
            _device = _devMgr.OpenByIdOrDefault(null);

            _engineFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
            _masterMixer = new MixingSampleProvider(_engineFormat) { ReadFully = true };

            _output = new WasapiOut(_device, AudioClientShareMode.Shared, false, 20);
            _output.Init(_masterMixer);
            _output.Play();

            CreateBus("Master");
            _log.Info($"AudioEngine ready. {sampleRate}Hz, stereo.");
        }

        public WaveFormat EngineFormat => _engineFormat;

        public ListenerState Listener
        {
            get { using var _ = _rw.Read(); return _listener; }
            set { using var _ = _rw.Write(); _listener = value; }
        }

        public AudioBus CreateBus(string name)
        {
            using var _ = _rw.Write();
            if (_buses.ContainsKey(name)) throw new InvalidOperationException($"Bus '{name}' exists.");
            var bus = new AudioBus(name, _engineFormat, _log);
            _buses.Add(name, bus);
            _masterMixer.AddMixerInput(bus);
            return bus;
        }

        public AudioBus GetBus(string name)
        {
            using var _ = _rw.Read();
            return _buses[name];
        }

        public bool TryGetBus(string name, out AudioBus? bus)
        {
            using var _ = _rw.Read();
            return _buses.TryGetValue(name, out bus);
        }

        public SoundSource CreateSoundFromFile(string name, string path)
        {
            var decoder = DecoderFactory.FromFile(path);
            return new SoundSource(name, decoder, _engineFormat, _log);
        }
        
        public SoundSource Create3DSoundFromFile(string name, string path, 
            SpatialParams spatialParams = null, string busName = "Master")
        {
            var soundSource = CreateSoundFromFile(name, path);
        
            soundSource.Enable3D(spatialParams, GetListener);
        
            AttachSourceToBus(soundSource, busName, autoPlay: false);
        
            return soundSource;
        }

        public void PlayOneShot(string path, string busName = "Master", float volume = 1.0f)
        {
            var snd = CreateSoundFromFile(System.IO.Path.GetFileNameWithoutExtension(path), path);
            snd.Volume = volume;
            snd.Loop = false;
            snd.Play();
            var bus = GetBus(busName);
            bus.AddInput(snd.Output);
        }

        public void AttachSourceToBus(SoundSource src, string busName = "Master", bool autoPlay = true)
        {
            var bus = GetBus(busName);
            bus.AddInput(src.Output);
            if (autoPlay) src.Play();
        }

        public void SetMasterVolume(float vol)
        {
            if (TryGetBus("Master", out var bus))
                bus!.SetVolume(vol);
        }

        public void SwitchDevice(string? deviceId)
        {
            using var _ = _rw.Write();
            _output?.Stop();
            _output?.Dispose();

            _device = _devMgr.OpenByIdOrDefault(deviceId);
            _output = new WasapiOut(_device, AudioClientShareMode.Shared, false, 20);
            _output.Init(_masterMixer);
            _output.Play();
            _log.Info("Audio device switched.");
        }

        public void Dispose()
        {
            foreach (var b in _buses.Values) b.Dispose();
            _output?.Stop();
            _output?.Dispose();
            _devMgr.Dispose();
        }

        /// <summary>
        /// 提供给空间化模块获取监听者状态
        /// </summary>
        public ListenerState GetListener() => Listener;
    }
}
