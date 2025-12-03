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
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Sound;

namespace Fulcrum.Engine
{
    public sealed class EngineCoordinator : IDisposable
    {
        private readonly AudioEngine _audioEngine;
        private readonly RendererBase _renderer;
        private readonly ILogger _log;
        
        private readonly Dictionary<string, SpatialObject> _spatialObjects = new();
        private readonly object _syncLock = new object();
        
        private long _frameCount = 0;
        private DateTime _lastUpdateTime = DateTime.Now;
        private Vector3 _lastCameraPosition;
        private DateTime _lastCameraUpdateTime;

        public EngineCoordinator(AudioEngine audioEngine, RendererBase renderer, ILogger logger = null)
        {
            _audioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _log = logger ?? new ConsoleLogger("EngineCoordinator");

            _renderer.OnUpdate += OnRenderUpdate;
            
            _log.Info("EngineCoordinator initialized");
        }

        public void RegisterSpatialObject(string objectId, SpatialObject spatialObject)
        {
            lock (_syncLock)
            {
                _spatialObjects[objectId] = spatialObject;
                
                if (spatialObject.SoundSource != null && spatialObject.SpatialParams != null)
                {
                    spatialObject.SpatialParams.Position = spatialObject.Position;
                    spatialObject.SpatialParams.Velocity = spatialObject.Velocity;
                    
                    spatialObject.SoundSource.Enable3D(spatialObject.SpatialParams, _audioEngine.GetListener);
                }
                
                _log.Debug($"Registered spatial object: {objectId} at {spatialObject.Position}");
            }
        }

        public void UpdateSpatialObject(string objectId, Vector3 worldPosition, Vector3 worldVelocity)
        {
            lock (_syncLock)
            {
                if (_spatialObjects.TryGetValue(objectId, out var spatialObject))
                {
                    spatialObject.Position = worldPosition;
                    spatialObject.Velocity = worldVelocity;
                    
                    if (spatialObject.SoundSource != null && spatialObject.SpatialParams != null)
                    {
                        spatialObject.SpatialParams.Position = worldPosition;
                        spatialObject.SpatialParams.Velocity = worldVelocity;
                    }
                }
            }
        }

        private void OnRenderUpdate(RendererBase renderer)
        {
            try
            {
                _frameCount++;
                var currentTime = DateTime.Now;
                var deltaTime = (float)(currentTime - _lastUpdateTime).TotalSeconds;
                _lastUpdateTime = currentTime;

                SyncListenerState(renderer.Camera);
            }
            catch (Exception ex)
            {
            }
        }

        private void SyncListenerState(Camera camera)
        {
            if (camera == null) return;

            Vector3 cameraVelocity = Vector3.Zero;
            var currentTime = DateTime.Now;
            var deltaTime = (float)(currentTime - _lastCameraUpdateTime).TotalSeconds;
            
            if (deltaTime > 0 && _lastCameraUpdateTime != DateTime.MinValue)
            {
                cameraVelocity = (camera.Position - _lastCameraPosition) / deltaTime;
            }

            var forward = Vector3.Normalize(camera.Target - camera.Position);
            
            var listenerState = new ListenerState(
                camera.Position,
                cameraVelocity,
                forward,
                camera.Up
            );

            _audioEngine.Listener = listenerState;

            _lastCameraPosition = camera.Position;
            _lastCameraUpdateTime = currentTime;
        }

        public SoundSource CreateStaticSound(string objectId, string soundPath, Vector3 worldPosition, 
            string busName = "Master", SpatialParams spatialParams = null)
        {
            var soundSource = _audioEngine.CreateSoundFromFile(objectId, soundPath);
            
            spatialParams ??= new SpatialParams
            {
                Position = worldPosition,
                MinDistance = 5.0f,
                MaxDistance = 100.0f,
                Rolloff = 2.0f
            };
            spatialParams.Position = worldPosition;

            var spatialObject = new SpatialObject
            {
                Id = objectId,
                Position = worldPosition,
                Velocity = Vector3.Zero,
                SoundSource = soundSource,
                SpatialParams = spatialParams
            };

            RegisterSpatialObject(objectId, spatialObject);
            
            _audioEngine.AttachSourceToBus(soundSource, busName, autoPlay: false);

            return soundSource;
        }

        public SoundSource CreateDynamicSound(string objectId, string soundPath, Vector3 initialPosition, 
            string busName = "Master", SpatialParams spatialParams = null)
        {
            var soundSource = CreateStaticSound(objectId, soundPath, initialPosition, busName, spatialParams);
            return soundSource;
        }

        public Vector3 GetSoundRelativePosition(string objectId)
        {
            lock (_syncLock)
            {
                if (_spatialObjects.TryGetValue(objectId, out var spatialObject))
                {
                    var listener = _audioEngine.Listener;
                    return spatialObject.Position - listener.Position;
                }
                return Vector3.Zero;
            }
        }

        public void Dispose()
        {
            if (_renderer != null)
            {
                _renderer.OnUpdate -= OnRenderUpdate;
            }
            
            lock (_syncLock)
            {
                _spatialObjects.Clear();
            }
        }
    }

    public class SpatialObject
    {
        public string Id { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public SoundSource SoundSource { get; set; }
        public SpatialParams SpatialParams { get; set; }
    }
}