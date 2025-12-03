// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License
// as published by the Free Software Foundation, version 3 (GPLv3 only).
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
using Fulcrum.Common;
using Fulcrum.ConfigSystem;
using Fulcrum.Engine.App;
using Fulcrum.Engine.Debug;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Sound;
using Fulcrum.Engine.Physics;
using Fulcrum.Engine.Scene;
using Veldrid;

using BepuPhysics;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;

namespace Fulcrum.Engine
{
    public class Initializer
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("Initializer");

        private static PixelFormat ParsePixelFormat(string name)
        {
            if (Enum.TryParse(name, ignoreCase: true, out PixelFormat fmt))
            {
                return fmt;
            }

            LOGGER.Warn($"Unknown PixelFormat '{name}', fallback to D24_UNorm_S8_UInt");
            return PixelFormat.D24_UNorm_S8_UInt;
        }

        private static GraphicsBackend ParseBackend(string name)
        {
            if (Enum.TryParse(name, ignoreCase: true, out GraphicsBackend backend))
            {
                return backend;
            }

            LOGGER.Warn($"Unknown GraphicsBackend '{name}', fallback to Vulkan");
            return GraphicsBackend.Vulkan;
        }

        /// <summary>
        /// 初始化 BEPU 物理引擎
        /// </summary>
        private static void InitializePhysics()
        {
            LOGGER.Info("Initializing Physics Engine (BEPU)...");

            if (PhysicsWorld.Initialized)
            {
                LOGGER.Warn("PhysicsWorld already initialized, skip duplicate init.");
                return;
            }

            var bufferPool = new BufferPool();

            var spring = new SpringSettings(30f, 1f);

            var narrowPhase = new FulcrumNarrowPhaseCallbacks(
                friction: 1.0f,
                maxRecVel: 2.0f,
                spring: spring);

            var poseIntegrator = new FulcrumPoseIntegratorCallbacks(
                gravity: new Vector3(0, -9.81f, 0),
                linDamp: 0.03f,
                angDamp: 0.03f);

            var solveDescription = new SolveDescription(
                velocityIterationCount: 8,
                substepCount: 1);

            var simulation = Simulation.Create(
                bufferPool,
                narrowPhase,
                poseIntegrator,
                solveDescription);

            PhysicsWorld.Attach(simulation, bufferPool, dispatcher: null);

            LOGGER.Info("Physics Engine initialized and attached to PhysicsWorld.");
        }

        public static void startup()
        {
            LOGGER.Info("Preparing ConfigSystem and default config entries...");

            // Renderer 配置总线
            var rendererBus = ConfigManager.GetOrCreateBus("renderer");

            var r_WindowTitle        = rendererBus.Register("WindowTitle",        "Fulcrum");
            var r_WindowWidth        = rendererBus.Register("WindowWidth",        768);
            var r_WindowHeight       = rendererBus.Register("WindowHeight",       768);

            var r_DepthFormat        = rendererBus.Register("DepthFormat",        PixelFormat.D24_UNorm_S8_UInt.ToString());
            var r_Backend            = rendererBus.Register("Backend",            GraphicsBackend.Vulkan.ToString());
            var r_EnableValidation   = rendererBus.Register("EnableValidation",   false);

            var r_ClearColorR        = rendererBus.Register("ClearColorR",        RgbaFloat.CornflowerBlue.R);
            var r_ClearColorG        = rendererBus.Register("ClearColorG",        RgbaFloat.CornflowerBlue.G);
            var r_ClearColorB        = rendererBus.Register("ClearColorB",        RgbaFloat.CornflowerBlue.B);
            var r_ClearColorA        = rendererBus.Register("ClearColorA",        RgbaFloat.CornflowerBlue.A);

            var r_TargetFps          = rendererBus.Register("TargetFps",          75);
            var r_StartMouseLocked   = rendererBus.Register("StartMouseLocked",   true);
            var r_MouseSensitivity   = rendererBus.Register("MouseSensitivity",   0.0012f);
            var r_BaseMoveSpeed      = rendererBus.Register("BaseMoveSpeed",      6.0f);
            var r_RunSpeedMultiplier = rendererBus.Register("RunSpeedMultiplier", 2.2f);

            // Audio 配置总线
            var audioBus = ConfigManager.GetOrCreateBus("audio");

            var a_MasterVolume        = audioBus.Register("MasterVolume", 1.0f);

            try
            {
                ConfigManager.LoadAll();
                LOGGER.Info($"ConfigManager initialized. Root = {Global.GameConfigPath}");
            }
            catch (Exception e)
            {
                LOGGER.Warn($"ConfigManager.LoadAll failed: {e}");
            }

            LOGGER.Info("Initializing All Sub-Engine...");

            // 音频引擎初始化
            LOGGER.Info("Initializing Audio Engine...");

            FulcrumEngine.AudioEngine = new AudioEngine();
            FulcrumEngine.AudioEngine.SetMasterVolume(a_MasterVolume.Value);

            FulcrumEngine.AudioEngine.CreateBus("MUSIC");
            FulcrumEngine.AudioEngine.CreateBus("SFX");

            // 渲染器初始化
            LOGGER.Info("Initializing Renderer...");

            var rendererConfig = new RendererConfig
            {
                WindowTitle    = r_WindowTitle.Value,
                WindowWidth    = r_WindowWidth.Value,
                WindowHeight   = r_WindowHeight.Value,
                ShaderDirectory = Path.Combine(Global.GameConfigPath, "shaders"),
                DepthFormat    = ParsePixelFormat(r_DepthFormat.Value),
                ClearColor     = new RgbaFloat(
                    r_ClearColorR.Value,
                    r_ClearColorG.Value,
                    r_ClearColorB.Value,
                    r_ClearColorA.Value
                ),
                EnableValidation = r_EnableValidation.Value,
                Backend          = ParseBackend(r_Backend.Value)
            };

            FulcrumEngine.RenderApp = new RenderApp(new RenderApp.Options
            {
                Config             = rendererConfig,
                TargetFps          = r_TargetFps.Value,
                StartMouseLocked   = r_StartMouseLocked.Value,
                MouseSensitivity   = r_MouseSensitivity.Value,
                BaseMoveSpeed      = r_BaseMoveSpeed.Value,
                RunSpeedMultiplier = r_RunSpeedMultiplier.Value,
                BeforeUpdate       = (app, rb) => { /* e.g. UI input, ECS tick */ },
                AfterUpdate        = (app, rb) => { /* e.g. debug draw */ }
            });

            InitializePhysics();

            FulcrumEngine.Initialize();

            // 初始化调试器
            RenderDebugGUI.Upd(FulcrumEngine.RenderApp);
            AudioDebugGUI.Upd(FulcrumEngine.AudioEngine);
            EngineDebugGUI.Upd(FulcrumEngine.RenderApp);
            SceneDebugGUI.Upd(FulcrumEngine.RenderApp);
            ConfigDebugGUI.Upd(FulcrumEngine.RenderApp);
            InputDebugGUI.Upd(FulcrumEngine.RenderApp);
            PhysicsDebugGUI.Upd(FulcrumEngine.RenderApp);

            // 启用主循环
            FulcrumEngine.RunMainTick();
        }
    }
}
