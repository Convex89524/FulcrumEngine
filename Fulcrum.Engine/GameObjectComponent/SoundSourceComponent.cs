﻿// Copyright (C) 2025-2029 Convex89524
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
using System.IO;
using System.Numerics;
using CMLS.CLogger;
using Fulcrum.Engine.App;
using Fulcrum.Engine.Scene;
using Fulcrum.Engine.Sound;

namespace Fulcrum.Engine.GameObjectComponent
{
    public class SoundSourceComponent : Component, ISceneSerializableComponent, IDisposable
    {
        #region 可序列化参数

        /// <summary>声音文件路径</summary>
        public string SoundPath { get; set; } = string.Empty;

        /// <summary>AudioBus</summary>
        public string BusName { get; set; } = "SFX";

        /// <summary>组件启用时是否自动播放</summary>
        public bool AutoPlayOnStart { get; set; } = false;

        /// <summary>循环播放</summary>
        public bool Loop { get; set; } = false;

        /// <summary>音量</summary>
        public float Volume { get; set; } = 1.0f;

        /// <summary>播放速度 1.0 为正常</summary>
        public float Speed { get; set; } = 0.99999f;

        /// <summary>3D 最小距离</summary>
        public float MinDistance { get; set; } = 1.0f;

        /// <summary>3D 最大距离</summary>
        public float MaxDistance { get; set; } = 64.0f;

        /// <summary>距离衰减因子</summary>
        public float Rolloff { get; set; } = 1.0f;

        #endregion

        #region 运行时状态

        private static readonly Clogger LOGGER = LogManager.GetLogger("SoundSourceComponent");

        private readonly Clogger _logger;
        private string? _objectId;
        private Vector3 _lastPosition;
        private bool _hasLastPosition;

        public SoundSource? RuntimeSource { get; private set; }

        #endregion

        public SoundSourceComponent()
        {
            _logger = LogManager.GetLogger("SoundSourceComponent");
        }

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();

            if (RuntimeSource != null && AutoPlayOnStart)
            {
                RuntimeSource.Play();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RuntimeSource?.Stop();
        }

        public void Dispose()
        {
            RuntimeSource?.Dispose();
            RuntimeSource = null;
        }

        #endregion

        #region 运行时创建 / 更新

        public void CreateRuntimeSource()
        {
            if (RuntimeSource != null)
            {
                RuntimeSource.Dispose();
                RuntimeSource = null;
            }

            if (string.IsNullOrWhiteSpace(SoundPath))
            {
                _logger.Warn("SoundPath is empty, skip creating sound source.");
                return;
            }

            var renderer = FulcrumEngine.RenderApp?.Renderer;
            if (renderer == null)
            {
                _logger.Warn("RenderApp.Renderer is null, SoundSourceComponent cannot create sound source.");
                return;
            }

            var coordinator = renderer.GetEngineCoordinator();
            if (coordinator == null)
            {
                _logger.Warn("EngineCoordinator is null, SoundSourceComponent cannot create sound source.");
                return;
            }

            var pos = Owner?.Transform?.Position ?? Vector3.Zero;

            if (string.IsNullOrWhiteSpace(_objectId))
            {
                _objectId = $"{Owner?.Name ?? "GameObject"}_sound_{Guid.NewGuid():N}";
            }

            var busName = string.IsNullOrWhiteSpace(BusName) ? "Master" : BusName;

            var spatialParams = new SpatialParams
            {
                Position = pos,
                Velocity = Vector3.Zero,
                MinDistance = MinDistance,
                MaxDistance = MaxDistance,
                Rolloff = Rolloff
            };

            try
            {
                RuntimeSource = coordinator.CreateStaticSound(
                    objectId: _objectId,
                    soundPath: SoundPath,
                    worldPosition: pos,
                    busName: busName,
                    spatialParams: spatialParams);

                RuntimeSource.Loop = Loop;
                RuntimeSource.Volume = ClampVolume(Volume);
                RuntimeSource.Speed = Speed;

                if (AutoPlayOnStart && Enabled)
                    RuntimeSource.Play();

                _lastPosition = pos;
                _hasLastPosition = true;

                _logger.Info($"Created 3D static sound for GameObject '{Owner?.Name}' with id '{_objectId}', path '{SoundPath}'.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to create 3D sound for GameObject '{Owner?.Name}': {ex}");
            }
        }

        #endregion

        #region 每帧更新

        protected override void Update(double dt)
        {
            base.Update(dt);

            if (Owner?.Transform == null)
                return;

            var renderer = FulcrumEngine.RenderApp?.Renderer;
            if (renderer == null)
                return;

            var coordinator = renderer.GetEngineCoordinator();
            if (coordinator == null)
                return;

            if (RuntimeSource == null)
            {
                if (!string.IsNullOrWhiteSpace(SoundPath))
                    CreateRuntimeSource();
                else
                    return;
            }

            if (string.IsNullOrWhiteSpace(_objectId))
                return;

            var pos = Owner.Transform.Position;
            Vector3 vel = Vector3.Zero;

            if (_hasLastPosition && dt > 0.0)
            {
                vel = (pos - _lastPosition) / (float)dt;
            }
            else
            {
                _hasLastPosition = true;
            }

            _lastPosition = pos;

            coordinator.UpdateSpatialObject(_objectId, pos, vel);

            RuntimeSource!.Volume = ClampVolume(Volume);
            RuntimeSource.Speed = Speed;
            RuntimeSource.Loop = Loop;
        }

        private static float ClampVolume(float vol)
        {
            if (vol < 0f) return 0f;
            if (vol > 4f) return 4f;
            return vol;
        }

        #endregion

        #region ISceneSerializableComponent

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SoundPath ?? string.Empty);
            writer.Write(BusName ?? string.Empty);

            writer.Write(AutoPlayOnStart);
            writer.Write(Loop);
            writer.Write(Volume);
            writer.Write(Speed);

            writer.Write(MinDistance);
            writer.Write(MaxDistance);
            writer.Write(Rolloff);
        }

        public void Deserialize(BinaryReader reader)
        {
            SoundPath       = reader.ReadString();
            BusName         = reader.ReadString();

            AutoPlayOnStart = reader.ReadBoolean();
            Loop            = reader.ReadBoolean();
            Volume          = reader.ReadSingle();
            Speed           = reader.ReadSingle();

            MinDistance     = reader.ReadSingle();
            MaxDistance     = reader.ReadSingle();
            Rolloff         = reader.ReadSingle();
        }

        #endregion
    }
}
