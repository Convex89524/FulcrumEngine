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
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Fulcrum.Engine.Sound
{
    /// <summary>
    /// 可堆叠效果链接口
    /// </summary>
    public interface IAudioEffect : ISampleProvider, IDisposable
    {
        ISampleProvider Source { get; }
        void SetSource(ISampleProvider source);
    }

    /// <summary>
    /// 音频总线：管理子音源/子总线、效果链、独立音量
    /// </summary>
    public sealed class AudioBus : ISampleProvider, IDisposable
    {
        private readonly object _sync = new();
        private readonly ILogger _log;
        private readonly MixingSampleProvider _mixer;
        private readonly List<IAudioEffect> _effects = new();
        private float _volume = 1.0f;

        public string Name { get; }
        public WaveFormat WaveFormat => _effects.Count == 0 ? _mixer.WaveFormat : _effects[^1].WaveFormat;

        public AudioBus(string name, WaveFormat format, ILogger log)
        {
            Name = name;
            _log = log;
            _mixer = new MixingSampleProvider(format) { ReadFully = true };
        }

        public void AddInput(ISampleProvider provider)
        {
            lock (_sync) { _mixer.AddMixerInput(provider); }
        }

        public void RemoveInput(ISampleProvider provider)
        {
            lock (_sync) { _mixer.RemoveMixerInput(provider); }
        }

        public void SetVolume(float volume) => _volume = Math.Clamp(volume, 0f, 4f);

        public void AddEffect(IAudioEffect effect)
        {
            lock (_sync)
            {
                if (_effects.Count == 0) effect.SetSource(_mixer);
                else effect.SetSource(_effects[^1]);
                _effects.Add(effect);
                _log.Info($"[{Name}] Effect added: {effect.GetType().Name}");
            }
        }

        public void RemoveLastEffect()
        {
            lock (_sync)
            {
                if (_effects.Count == 0) return;
                var last = _effects[^1];
                _effects.RemoveAt(_effects.Count - 1);
                last.Dispose();
                for (int i = 0; i < _effects.Count; i++)
                {
                    _effects[i].SetSource(i == 0 ? (ISampleProvider)_mixer : _effects[i - 1]);
                }
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = _effects.Count == 0
                ? _mixer.Read(buffer, offset, count)
                : _effects[^1].Read(buffer, offset, count);

            for (int i = 0; i < read; i++) buffer[offset + i] *= _volume;
            return read;
        }

        public void Dispose()
        {
            foreach (var e in _effects) e.Dispose();
        }
    }
}
