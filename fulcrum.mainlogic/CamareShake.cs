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

using CMLS.CLogger;
using Fulcrum.Engine;
using Fulcrum.Engine.App;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Sound;
using Vector3 = System.Numerics.Vector3;

namespace fulcrum.mainlogic
{
    public class EarthquakeCameraShake : ScriptBase
    {
        public override string ScriptId => "EarthquakeCameraShake";

        private Clogger _logger;
        private RendererBase _renderer;
        private Camera _camera;

        private float _time = 0f;
        private float _duration = 0f;
        private float _maxAmp = 0f;
        private float _speed = 0f;
        private float _riseTime = 0f;
        private float _fallTime = 0f;

        private bool _shakePos = false;
        private bool _shakeRot = false;
        private bool _shakeFov = false;

        private Random _rnd = new Random();
        private float _seedX, _seedY, _seedZ;

        private float _originFov;
        private Vector3 _lastOffsetPos = Vector3.Zero;
        private Vector3 _lastRotOffset = Vector3.Zero;
        private float _lastFovOffset = 0f;
        
        private bool _running = false;

        public override void OnLoad(Clogger logger, RenderApp app, AudioEngine audio)
        {
            _logger = logger;
            _renderer = app.Renderer;
            _camera = _renderer.Camera;

            _originFov = _camera.FieldOfView;

            _seedX = (float)_rnd.NextDouble() * 9999f;
            _seedY = (float)_rnd.NextDouble() * 9999f;
            _seedZ = (float)_rnd.NextDouble() * 9999f;

            _logger.Info("[EQ] Earthquake script loaded");
        }

        public override void OnUpdate(int tick, AudioEngine audio)
        {
        }

        public override void OnRenderFrame(RendererBase renderer)
        {
            if (!_running) return;
            if (_time <= 0f) return;

            float delta = renderer.GetDeltaTime();
            _time -= delta;

            float t = Mathf.Clamp01(CalcNormalizedTime());

            float amp = _maxAmp * t;

            float nx = SmoothNoise(_seedX + renderer.GetDeltaTime() * _speed);
            float ny = SmoothNoise(_seedY + renderer.GetDeltaTime() * _speed);
            float nz = SmoothNoise(_seedZ + renderer.GetDeltaTime() * _speed);

            // --- ROTATION 摇动 ---
            if (_shakeRot)
            {
                Vector3 rotOffset = new Vector3(nx, ny, 0) * (amp * 0.4f);
                _camera.SetRotation(
                    _camera.GetYaw() - _lastRotOffset.Y + rotOffset.Y,
                    _camera.GetPitch() - _lastRotOffset.X + rotOffset.X
                );
                _lastRotOffset = rotOffset;
            }

            // --- FOV 摇动 ---
            if (_shakeFov)
            {
                float fovOffset = nz * amp * 0.15f;
                _camera.FieldOfView = _originFov - _lastFovOffset + fovOffset;
                _lastFovOffset = fovOffset;
            }

            if (_time <= 0f)
                ResetOffsets();
        }

        public override void OnUninstall()
        {
            ResetOffsets();
            _logger.Info("[EQ] Earthquake script unloaded");
        }

        public void StartShake(
            string targets,
            float speed,
            float maxAmplitude,
            float duration,
            float riseTime,
            float fallTime)
        {
            StopShake();

            _shakePos = targets.Contains("pos");
            _shakeRot = targets.Contains("rot");
            _shakeFov = targets.Contains("fov");

            _speed = speed;
            _maxAmp = maxAmplitude;
            _duration = duration;
            _riseTime = riseTime;
            _fallTime = fallTime;

            _time = riseTime + duration + fallTime;
            _running = true;

            _logger.Info($"[EQ] StartShake targets={targets}, amp={maxAmplitude}, duration={duration}, rise={riseTime}, fall={fallTime}");
        }
        
        public void StopShake()
        {
            if (!_running) return;
            
            if (_shakeRot)
                _camera.SetRotation(
                    _camera.GetYaw() - _lastRotOffset.Y,
                    _camera.GetPitch() - _lastRotOffset.X
                );

            if (_shakeFov)
                _camera.FieldOfView = _originFov;

            _lastOffsetPos = Vector3.Zero;
            _lastRotOffset = Vector3.Zero;
            _lastFovOffset = 0f;

            _running = false;
            _time = 0f;

            _logger.Info("[EQ] Shake stopped and camera restored");
        }

        private float CalcNormalizedTime()
        {
            float total = _riseTime + _duration + _fallTime;
            float passed = total - _time;

            if (passed <= _riseTime)
                return passed / _riseTime;

            if (passed <= _riseTime + _duration)
                return 1f;

            float fallPassed = passed - (_riseTime + _duration);
            return 1f - (fallPassed / _fallTime);
        }

        private float SmoothNoise(float x)
        {
            float ft = x - MathF.Floor(x);
            float v1 = (float)_rnd.NextDouble() * 2f - 1f;
            float v2 = (float)_rnd.NextDouble() * 2f - 1f;
            return Mathf.Lerp(v1, v2, ft);
        }
        
        private void ResetOffsets()
        {
            if (_shakePos)
            {
                _camera.SetPosition(_camera.Position - _lastOffsetPos, true);
            }

            if (_shakeRot)
            {
                _camera.SetRotation(
                    _camera.GetYaw() - _lastRotOffset.Y,
                    _camera.GetPitch() - _lastRotOffset.X
                );
            }

            if (_shakeFov)
            {
                _camera.FieldOfView = _originFov;
            }

            _lastOffsetPos = Vector3.Zero;
            _lastRotOffset = Vector3.Zero;
            _lastFovOffset = 0f;
        }

        private static class Mathf
        {
            public static float Clamp01(float v) => MathF.Min(1f, MathF.Max(0f, v));
            public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        }
    }
}