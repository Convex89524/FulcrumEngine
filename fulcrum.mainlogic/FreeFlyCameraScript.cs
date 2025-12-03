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

using System.Numerics;
using CMLS.CLogger;
using Fulcrum.Engine;
using Fulcrum.Engine.App;
using Fulcrum.Engine.GameObjectComponent;
using Fulcrum.Engine.InputSystem;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Scene;
using Fulcrum.Engine.Sound;
using Veldrid;

namespace fulcrum.mainlogic
{
    public sealed class FreeFlyCameraScript : ScriptBase
    {
        private Clogger _logger;
        private RenderApp _app;
        private RendererBase _renderer;
        private InputContext _input;

        private InputActionHandle _moveForward;
        private InputActionHandle _moveBackward;
        private InputActionHandle _moveLeft;
        private InputActionHandle _moveRight;
        private InputActionHandle _moveUp;
        private InputActionHandle _moveDown;

        private float _moveSpeed = 5.0f;

        public FreeFlyCameraScript()
        {
            ScriptId = "FreeFlyCamera";
        }

        public override void OnLoad(Clogger logger, RenderApp renderApp, AudioEngine audioEngine)
        {
            _logger   = logger;
            _app      = renderApp;
            _renderer = renderApp.Renderer;
            _input    = renderApp.Input;

            _moveForward  = _input.RegisterAction("camera.moveForward",  Key.W); // W
            _moveBackward = _input.RegisterAction("camera.moveBackward", Key.S); // S
            _moveLeft     = _input.RegisterAction("camera.moveLeft",     Key.A); // A
            _moveRight    = _input.RegisterAction("camera.moveRight",    Key.D); // D
            _moveUp       = _input.RegisterAction("camera.moveUp",       Key.E); // E
            _moveDown     = _input.RegisterAction("camera.moveDown",     Key.Q); // Q

            if (_renderer?.Camera != null)
            {
                _moveSpeed = _renderer.Camera.MoveSpeed;
            }

            _logger.Info("FreeFlyCameraScript loaded.");
        }

        public override void OnUpdate(int currentTick, AudioEngine audioEngine)
        {
        }

        public override void OnRenderFrame(RendererBase rendererBase)
        {
            if (_renderer == null || _input == null)
                return;

            float dt = _renderer.GetDeltaTime();

            CameraComponent camComp = SceneCameraBinder.MainCamera;
            Transform transform = camComp?.Owner?.Transform;
            Camera cam = null;

            if (camComp != null)
            {
                cam = camComp.RuntimeCamera ?? _renderer.Camera;
            }
            else
            {
                cam = _renderer.Camera;
            }

            if (cam == null)
                return;

            Vector3 forward, right, up;

            if (transform != null)
            {
                forward = Vector3.Normalize(transform.Forward);
                right   = Vector3.Normalize(transform.Right);
                up      = Vector3.Normalize(transform.Up);
            }
            else
            {
                forward = cam.Forward;
                right   = cam.Right;
                up      = cam.UpVector;
            }

            Vector3 dir = Vector3.Zero;

            if (_input.IsDown(_moveForward))  dir += forward;
            if (_input.IsDown(_moveBackward)) dir -= forward;

            if (_input.IsDown(_moveRight))    dir -= right;
            if (_input.IsDown(_moveLeft))     dir += right;

            if (_input.IsDown(_moveUp))       dir += up;
            if (_input.IsDown(_moveDown))     dir -= up;

            if (dir == Vector3.Zero)
                return;

            dir = Vector3.Normalize(dir);

            if (transform != null)
            {
                transform.Position += dir * _moveSpeed * dt;
            }
            else
            {
                cam.Move(dir, dt);
            }
        }

        public override void OnUninstall()
        {
        }
    }
}