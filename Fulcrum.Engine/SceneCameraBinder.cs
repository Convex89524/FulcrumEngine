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
using System.Linq;
using Fulcrum.Engine.GameObjectComponent;
using Fulcrum.Engine.Render;

namespace Fulcrum.Engine
{
    public static class SceneCameraBinder
    {
        private static RendererBase? _renderer;
        private static CameraComponent? _mainCamera;
        private static readonly List<CameraComponent> _allCameras = new();

        public static CameraComponent? MainCamera => _mainCamera;

        static SceneCameraBinder()
        {
            Scene.Scene.OnBinderSceneChanged += (obj,scene) =>
            {
                OnSceneChanged(scene);
            };
        }

        public static void Initialize(RendererBase renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            if (Scene.Scene.CurrentScene != null)
            {
                RefreshFromScene(Scene.Scene.CurrentScene);
            }
        }

        internal static void Register(CameraComponent cam)
        {
            if (cam == null) return;

            if (!_allCameras.Contains(cam))
                _allCameras.Add(cam);

            if (_mainCamera == null || cam.IsMainCamera)
            {
                if (cam.IsMainCamera && _mainCamera != null && _mainCamera != cam)
                {
                    _mainCamera.IsMainCamera = false;
                }

                _mainCamera = cam;
                ApplyCameraToRenderer();
            }
        }

        internal static void Unregister(CameraComponent cam)
        {
            if (cam == null) return;

            _allCameras.Remove(cam);

            if (_mainCamera == cam)
            {
                _mainCamera = null;

                if (Scene.Scene.CurrentScene != null)
                    RefreshFromScene(Scene.Scene.CurrentScene);
                else
                    ApplyCameraToRenderer();
            }
        }

        internal static void OnSceneChanged(Scene.Scene scene)
        {
            RefreshFromScene(scene);
        }

        private static void RefreshFromScene(Scene.Scene? scene)
        {
            if (scene == null)
            {
                _mainCamera = null;
                _allCameras.Clear();
                ApplyCameraToRenderer();
                return;
            }

            var cams = scene.FindObjectsOfType<CameraComponent>();
            _allCameras.Clear();
            _allCameras.AddRange(cams);

            if (cams.Count == 0)
            {
                _mainCamera = null;
                ApplyCameraToRenderer();
                return;
            }

            var main = cams.FirstOrDefault(c => c.IsMainCamera);
            _mainCamera = main ?? cams[0];

            ApplyCameraToRenderer();
        }

        private static void ApplyCameraToRenderer()
        {
            if (_renderer == null) return;

            if (_mainCamera?.RuntimeCamera != null)
            {
                _renderer.Camera = _mainCamera.RuntimeCamera;
            }
        }
    }
}