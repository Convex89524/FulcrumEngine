﻿// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License
// as published by
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
using System.Numerics;
using CMLS.CLogger;
using Fulcrum.Common;
using Fulcrum.Engine.App;
using Fulcrum.Engine.GameObjectComponent;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Scene;
using Fulcrum.Engine.Sound;

namespace Fulcrum.Engine
{
    public sealed class CoreMouseLookScript : ScriptBase
    {
        private Clogger _logger;
        private RenderApp _renderApp;
        private AudioEngine _audio;

        private bool _mouseLocked;
        private bool _mouseLockApplied;
        private bool _prevToggleDown;

        private float _yaw;
        private float _pitch;

        private Camera _cameraCache;

        public float Yaw => _yaw;

        public float Pitch => _pitch;

        public CoreMouseLookScript()
        {
            ScriptId = "engine.core.MouseLook";
        }

        public override void OnLoad(Clogger logger, RenderApp renderApp, AudioEngine audioEngine)
        {
            _logger    = logger;
            _renderApp = renderApp;
            _audio     = audioEngine;

            if (_renderApp?.Renderer == null)
            {
                _logger.Warn("CoreMouseLookScript loaded but RenderApp.Renderer is null.");
                return;
            }

            CameraComponent camComp = SceneCameraBinder.MainCamera;
            Transform camTransform = camComp?.Owner?.Transform;

            if (camComp != null)
            {
                _cameraCache = camComp.RuntimeCamera ?? _renderApp.Renderer.Camera;
            }
            else
            {
                _cameraCache = _renderApp.Renderer.Camera;
            }

            if (_cameraCache == null)
            {
                _logger.Warn("CoreMouseLookScript loaded but no active Camera is available.");
                return;
            }

            _renderApp.Input.RegisterAction(
                "core.toggleMouseLock",
                _renderApp.AppOptions.Bindings.ToggleMouseLock);

            SyncInitialYawPitch(_cameraCache, camTransform);

            _mouseLocked       = _renderApp.AppOptions.StartMouseLocked;
            _prevToggleDown    = false;
            _mouseLockApplied  = false;

            ApplyMouseLockState(_mouseLocked, force: true);

            _logger.Info($"CoreMouseLookScript loaded. StartMouseLocked = {_mouseLocked}");
        }

        public override void OnUpdate(int currentTick, AudioEngine audioEngine)
        {
        }

        public override void OnRenderFrame(RendererBase rendererBase)
        {
            if (_renderApp == null)
                return;

            var renderer = rendererBase ?? _renderApp.Renderer;
            if (renderer == null)
                return;

            if (renderer.Camera == null)
            {
                if (_cameraCache != null)
                {
                    renderer.Camera = _cameraCache;
                }
                else
                {
                    _logger?.Warn("CoreMouseLookScript: Renderer.Camera is null in OnRenderFrame.");
                    return;
                }
            }

            _cameraCache = renderer.Camera;

            HandleMouseLockToggle();
            HandleMouseLook(renderer);
        }

        public override void OnUninstall()
        {
            try
            {
                SdlCompat.ShowCursor(1);
                SdlCompat.SetRelativeMouseMode(false);
            }
            catch (Exception ex)
            {
                _logger?.Warn($"CoreMouseLookScript.OnUninstall reset failed: {ex.Message}");
            }
        }

        private void HandleMouseLockToggle()
        {
            if (_renderApp?.Input == null)
                return;

            bool toggleNow = _renderApp.Input.IsDown("core.toggleMouseLock");

            if (toggleNow && !_prevToggleDown)
            {
                _mouseLocked = !_mouseLocked;
                ApplyMouseLockState(_mouseLocked);
            }

            _prevToggleDown = toggleNow;
        }

        private void HandleMouseLook(RendererBase renderer)
        {
            if (!_mouseLocked)
                return;

            if (renderer == null)
                return;

            CameraComponent camComp = SceneCameraBinder.MainCamera;
            Transform camTransform = camComp?.Owner?.Transform;

            if (camComp == null && renderer.Camera == null)
            {
                _logger?.Warn("CoreMouseLookScript.HandleMouseLook: no CameraComponent and Renderer.Camera is null.");
                return;
            }

            int dx, dy;
            SdlCompat.GetRelativeMouseState(out dx, out dy);

            if (dx == 0 && dy == 0)
                return;

            float sensitivity = _renderApp.AppOptions.MouseSensitivity;

            _yaw   += dx * sensitivity;
            _pitch -= dy * sensitivity;

            float pitchLimit = MathF.PI / 2.0f - 0.1f;
            _pitch = Math.Clamp(_pitch, -pitchLimit, pitchLimit);

            if (camTransform != null)
            {
                Vector3 front;
                front.X = MathF.Cos(_yaw) * MathF.Cos(_pitch);
                front.Y = MathF.Sin(_pitch);
                front.Z = MathF.Sin(_yaw) * MathF.Cos(_pitch);

                front = Vector3.Normalize(front);
                if (front.LengthSquared() > 1e-6f)
                {
                    var pos = camTransform.Position;
                    camTransform.LookAt(pos + front, Vector3.UnitY);
                }
            }
            else
            {
                renderer.Camera.SetRotation(_yaw, _pitch);
            }

            SdlCompat.WarpMouseToCenter(
                renderer.WindowHandle,
                _renderApp.AppOptions.Config.WindowWidth,
                _renderApp.AppOptions.Config.WindowHeight);
        }

        private void SyncInitialYawPitch(Camera cam, Transform camTransform)
        {
            if (cam == null)
                return;

            Vector3 forward;

            if (camTransform != null)
            {
                forward = Vector3.Normalize(camTransform.Forward);
            }
            else
            {
                forward = Vector3.Normalize(cam.Target - cam.Position);
            }

            if (forward.LengthSquared() < 1e-8f)
                forward = Vector3.UnitZ;

            _yaw = MathF.Atan2(forward.Z, forward.X);
            _pitch = MathF.Asin(Math.Clamp(forward.Y, -1f, 1f));

            if (camTransform != null)
            {
                Vector3 front;
                front.X = MathF.Cos(_yaw) * MathF.Cos(_pitch);
                front.Y = MathF.Sin(_pitch);
                front.Z = MathF.Sin(_yaw) * MathF.Cos(_pitch);

                front = Vector3.Normalize(front);
                if (front.LengthSquared() > 1e-6f)
                {
                    var pos = camTransform.Position;
                    camTransform.LookAt(pos + front, Vector3.UnitY);
                }
            }
            else
            {
                cam.SetRotation(_yaw, _pitch);
            }
        }

        private void ApplyMouseLockState(bool locked, bool force = false)
        {
            if (!force && locked == _mouseLockApplied)
                return;

            try
            {
                SdlCompat.SetRelativeMouseMode(locked);
                SdlCompat.ShowCursor(locked ? 0 : 1);

                if (locked && _renderApp?.Renderer != null)
                {
                    SdlCompat.WarpMouseToCenter(
                        _renderApp.Renderer.WindowHandle,
                        _renderApp.AppOptions.Config.WindowWidth,
                        _renderApp.AppOptions.Config.WindowHeight);
                }

                SdlCompat.PumpEvents();
                _mouseLockApplied = locked;
            }
            catch (Exception ex)
            {
                _logger?.Warn($"CoreMouseLookScript.ApplyMouseLockState failed: {ex.Message}");
            }
        }
    }
}
