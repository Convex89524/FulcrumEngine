// Copyright (C) 2025-2029 Convex89524
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
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using CMLS.CLogger;
using Fulcrum.Common;
using Fulcrum.ConfigSystem;
using Fulcrum.Engine.InputSystem;
using static Fulcrum.Engine.Render.VertexElementFormatExtensions;
using Fulcrum.Engine.Render;

namespace Fulcrum.Engine.App
{
    public sealed class RenderApp : IDisposable
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("RenderApp");

        // 模块生命周期
        public interface IModule : IDisposable
        {
            void OnLoad(RenderApp app, RendererBase renderer);
            void OnUpdate(RenderApp app, RendererBase renderer, float deltaTime);
        }

        // 可配置选项
        public sealed class Options
        {
            public RendererConfig Config = new RendererConfig
            {
                Backend        = GraphicsBackend.Vulkan,
                WindowWidth    = 1024,
                WindowHeight   = 768,
                WindowTitle    = "Fulcrum",
                ShaderDirectory = System.IO.Path.Combine(Global.GamePath, "shaders"),
                DepthFormat    = PixelFormat.D24_UNorm_S8_UInt,
                ClearColor     = RgbaFloat.CornflowerBlue
            };

            public int TargetFps = 60;

            public Vector3 CameraPosition = new Vector3(3, 3, 3);
            public Vector3 CameraTarget   = Vector3.Zero;
            public Vector3 CameraUp       = Vector3.UnitY;
            public float FieldOfView      = MathF.PI / 3f;
            public float Near             = 0.1f;
            public float Far              = 1000f;

            public InputBindings Bindings = new InputBindings();

            public bool StartMouseLocked   = true;
            public float MouseSensitivity  = 0.0015f;
            public float BaseMoveSpeed     = 5.0f;
            public float RunSpeedMultiplier = 2.5f;

            public Action<RenderApp, RendererBase>? BeforeUpdate;
            public Action<RenderApp, RendererBase>? AfterUpdate;
        }

        public Options AppOptions { get; }
        public RendererBase Renderer { get; private set; }
        public IReadOnlyList<IModule> Modules => _modules.AsReadOnly();

        private readonly List<IModule> _modules = new List<IModule>();

        public InputContext Input { get; }

        public sealed class InputBindings
        {
            public Key ToggleMouseLock = Key.Escape;
        }

        public RenderApp(Options? options = null)
        {
            LOGGER.Info("RenderApp Initializing.");
            AppOptions = options ?? new Options();

            Renderer = new RendererBase(AppOptions.Config);

            Input = new InputContext(Renderer, "input");

            var cam = new Camera
            {
                Position    = AppOptions.CameraPosition,
                Target      = AppOptions.CameraTarget,
                Up          = AppOptions.CameraUp,
                AspectRatio = (float)AppOptions.Config.WindowWidth / AppOptions.Config.WindowHeight,
                FieldOfView = AppOptions.FieldOfView,
                NearPlane   = AppOptions.Near,
                FarPlane    = AppOptions.Far,
                MoveSpeed   = AppOptions.BaseMoveSpeed
            };
            Renderer.Camera = cam;

            Renderer.SetFPS(AppOptions.TargetFps);

            Renderer.OnUpdate = (rb) =>
            {
                AppOptions.BeforeUpdate?.Invoke(this, rb);

                AppOptions.AfterUpdate?.Invoke(this, rb);

                float dt = rb.GetDeltaTime();
                for (int i = 0; i < _modules.Count; i++)
                {
                    _modules[i].OnUpdate(this, rb, dt);
                }
            };

            LOGGER.Info("Initialized successfully");
        }

        public RenderApp AddModule(IModule module)
        {
            _modules.Add(module);
            return this;
        }

        // 主循环
        public void Run()
        {
            LOGGER.Info("RenderApp main loop startup.");

            LOGGER.Debug("Loading modules...");
            foreach (var m in _modules)
            {
                m.OnLoad(this, Renderer);
                LOGGER.Debug($"Module loaded: {m}");
            }

            Renderer.Run();
        }

        public void Dispose()
        {
            foreach (var m in _modules)
            {
                try { m.Dispose(); }
                catch { /* ignore */ }
            }

            try { SdlCompat.ShowCursor(1); }          catch { /* ignore */ }
            try { SdlCompat.SetRelativeMouseMode(false); } catch { /* ignore */ }

            Renderer?.Dispose();
            Renderer = null;

            FulcrumEngine.Shutdown();
        }
    }
}
