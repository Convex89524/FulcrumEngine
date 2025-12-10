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
using System.Diagnostics;
using System.Reflection;
using BepuPhysics;
using BepuUtilities;
using BepuUtilities.Memory;
using CMLS.CLogger;
using Fulcrum.Common;
using Fulcrum.Engine.App;
using Fulcrum.Engine.GameObjectComponent.Phys;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Scene;
using Fulcrum.Engine.Sound;

namespace Fulcrum.Engine
{
    public static class FulcrumEngine
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("FulcrumEngine");

        public static Clogger GlobalLogger { get => LOGGER; }

        #region Sub-Engine

        // 渲染子引擎
        public static RenderApp RenderApp;

        // 音频子引擎
        public static AudioEngine AudioEngine;

        #endregion
        
        #region Sub-Module

        public static TextureManager TextureManager;
        
        private static EngineCoordinator _coordinator;
        
        #endregion

        private static readonly List<ScriptBase> LoadedScripts = new();

        public static bool IsRun = false;
        
        public static int ServerTick = 0;
        public static int ServerPhysTick = 0;

        private static int _tickRate = 50;
        private static int _phyTickRate = 200;
        private static double _targetFrameTimeMs => 1000.0 / _tickRate;
        private static double _phyTargetFrameTimeMs => 1000.0 / _phyTickRate;

        public static event Action<double> OnLogicTick;
        public static event Action OnEngineShutdown;

        public static EngineStartupOptions StartupOptions { get; private set; } = new EngineStartupOptions();

        public static Func<(Simulation simulation, BufferPool bufferPool, IThreadDispatcher dispatcher)> PhysicsBootstrapper { get; set; }

        public static void Initialize()
        {
            Initialize(new EngineStartupOptions());
        }

        public static void Initialize(EngineStartupOptions options)
        {
            StartupOptions = options ?? new EngineStartupOptions();

            LOGGER.Info("Initializing Fulcrum Engine...");
            
            _coordinator = new EngineCoordinator(AudioEngine, RenderApp.Renderer);
            RenderApp.Renderer.SetEngineCoordinator(_coordinator);

            bool sceneLoadedFromFile = false;
            if (StartupOptions.LoadSceneFromFile &&
                !string.IsNullOrWhiteSpace(StartupOptions.SceneFilePath))
            {
                try
                {
                    string fullPath = Path.GetFullPath(StartupOptions.SceneFilePath);
                    LOGGER.Info($"Try load Scene from binary file: {fullPath}");
                    var loadedScene = Scene.SceneSerializer.LoadFromFile(fullPath);
                    Scene.Scene.SetCurrent(loadedScene);
                    sceneLoadedFromFile = true;
                    LOGGER.Info("Scene loaded from file and set as CurrentScene.");
                }
                catch (Exception e)
                {
                    LOGGER.Error($"Failed to load scene from file \"{StartupOptions.SceneFilePath}\": {e}");
                    LOGGER.Warn("Fallback to default empty Scene.");
                }
            }

            if (Scene.Scene.CurrentScene == null)
            {
                Scene.Scene.SetCurrent(new Scene.Scene());
                LOGGER.Info("No current scene found. Created default Scene and set as CurrentScene.");
            }
            
            TextureManager = new TextureManager(RenderApp.Renderer);
            
            InitializePhysicsWorld();

            LoadAllScript();

            SceneTickBridge.Hook();
            
            RenderApp.Renderer.OnUpdate += (rb)=> SceneTickBridge.HandleOnRenderFrame(rb.GetDeltaTime());

            foreach (var script in LoadedScripts)
            {
                try
                {
                    LOGGER.Info($"Loading Script: {script.ScriptId}");
                    Clogger scriptLogger = LogManager.GetLogger("script:" + script.ScriptId);
                    script.OnLoad(scriptLogger, RenderApp, AudioEngine);
                }
                catch (Exception e)
                {
                    LOGGER.Error($"Script \"{script.ScriptId}\" failed OnLoad: {e}");
                    if (e.InnerException != null)
                        LOGGER.Error($"InnerException: {e.InnerException}");
                }
            }

            LOGGER.Info($"Total Loaded Scripts: {LoadedScripts.Count}");
        }

        /// <summary>
        /// 挂接 BEPU Simulation 到 PhysicsWorld。
        /// </summary>
        private static void InitializePhysicsWorld()
        {
            if (PhysicsWorld.Initialized)
            {
                LOGGER.Info("PhysicsWorld already initialized, skip physics bootstrap.");
                return;
            }

            if (PhysicsBootstrapper == null)
            {
                LOGGER.Warn("PhysicsBootstrapper is null. Physics world will NOT be initialized. " +
                            "If you need physics, please assign FulcrumEngine.PhysicsBootstrapper before calling Initialize().");
                return;
            }

            try
            {
                var (simulation, bufferPool, dispatcher) = PhysicsBootstrapper.Invoke();

                if (simulation == null || bufferPool == null)
                {
                    LOGGER.Warn("PhysicsBootstrapper returned null Simulation or BufferPool. Physics world not attached.");
                    return;
                }

                PhysicsWorld.Attach(simulation, bufferPool, dispatcher);
                LOGGER.Info("Physics world attached to PhysicsWorld successfully.");
            }
            catch (Exception ex)
            {
                LOGGER.Error($"InitializePhysicsWorld failed: {ex}");
            }
        }

        private static Thread MainTickThread;
        private static Thread PhysTickThread;

        public static void RunMainTick()
        {
            LOGGER.Info("Engine main tickLoop startup.");

            IsRun = true;
            var dtWatch = new Stopwatch();
            dtWatch.Start();

            MainTickThread = new Thread(() =>
            {
                double targetFrameTime;

                Stopwatch stopwatch = new Stopwatch();

                while (IsRun)
                {
                    stopwatch.Restart();

                    double deltaTime = dtWatch.Elapsed.TotalSeconds;
                    dtWatch.Restart();

                    Tick(deltaTime);

                    EngineEvents.RaiseLogicTick(deltaTime);

                    targetFrameTime = _targetFrameTimeMs;

                    double elapsed = stopwatch.Elapsed.TotalMilliseconds;
                    int sleepTime = (int)(targetFrameTime - elapsed);
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }

            });
            
            var physDtWatch = new Stopwatch();
            physDtWatch.Start();
            
            RigidBodyComponent.SyncAllToPhysics();
            
            PhysTickThread = new Thread(() =>
            {
                double targetFrameTime;

                Stopwatch stopwatch = new Stopwatch();

                while (IsRun)
                {
                    stopwatch.Restart();

                    double deltaTime = physDtWatch.Elapsed.TotalSeconds;
                    physDtWatch.Restart();

                    PhysTick(deltaTime);

                    EngineEvents.RaiseLogicTick(deltaTime);

                    targetFrameTime = _phyTargetFrameTimeMs;

                    double elapsed = stopwatch.Elapsed.TotalMilliseconds;
                    int sleepTime = (int)(targetFrameTime - elapsed);
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }
            });
            
            MainTickThread.Start();
            PhysTickThread.Start();

            RenderApp.Renderer.OnUpdate += (renderer) =>
            {
                RenderFrame(renderer);
            };

            RenderApp.Run();
        }

        public static int GetTickRate()
        {
            return _tickRate;
        }

        public static void SetTickRate(int rate)
        {
            if (rate <= 0)
            {
                LOGGER.Warn($"TickRate must be > 0, received {rate}, ignored.");
                return;
            }

            _tickRate = rate;
            LOGGER.Info($"TickRate updated to: {_tickRate} TPS");
        }

        public static void SaveCurrentScene(string filePath)
        {
            var scene = Scene.Scene.CurrentScene;
            if (scene == null)
            {
                LOGGER.Warn("SaveCurrentScene: CurrentScene is null, abort.");
                return;
            }

            try
            {
                string fullPath = Path.GetFullPath(filePath);
                Scene.SceneSerializer.SaveToFile(scene, fullPath);
                LOGGER.Info($"Current Scene saved to: {fullPath}");
            }
            catch (Exception e)
            {
                LOGGER.Error($"SaveCurrentScene failed: {e}");
            }
        }

        public static void LoadSceneFromFile(string filePath)
        {
            try
            {
                string fullPath = Path.GetFullPath(filePath);
                var loadedScene = Scene.SceneSerializer.LoadFromFile(fullPath);
                Scene.Scene.SetCurrent(loadedScene);
                LOGGER.Info($"Scene reloaded at runtime from: {fullPath}");
            }
            catch (Exception e)
            {
                LOGGER.Error($"LoadSceneFromFile failed: {e}");
            }
        }

        private static void LoadAllScript()
        {
            LOGGER.Info("Searching for scripts...");
            LoadedScripts.Clear();
            try
            {
                string basePath = Global.GamePath ?? "";
                string gameName = Global.GameFolderName ?? "Game";

                var candidates = new[]
                {
                    Path.Combine(basePath, "bin", "net9.0",           $"{gameName}.mainlogic.dll"),
                    Path.Combine(basePath, "bin", "net8.0",           $"{gameName}.mainlogic.dll"),
                    Path.Combine(basePath, "bin", "net8.0-windows",   $"{gameName}.mainlogic.dll")
                };

                string? dllPath = candidates.FirstOrDefault(File.Exists);
                if (dllPath == null)
                {
                    LOGGER.Warn($"No script assembly found. Probed:\n - {string.Join("\n - ", candidates)}");
                    return;
                }

                LOGGER.Info($"Target script assembly path: {dllPath}");
                Assembly externalAssembly = Assembly.LoadFrom(dllPath);
                LOGGER.Info($"Successfully loaded assembly: {externalAssembly.FullName}");

                var scriptTypes = externalAssembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ScriptBase)) && !t.IsAbstract);

                foreach (var type in scriptTypes)
                {
                    try
                    {
                        if (Activator.CreateInstance(type) is ScriptBase instance)
                        {
                            LoadedScripts.Add(instance);
                            LOGGER.Info($"Script registered: {instance.ScriptId} ({type.FullName})");
                        }
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error($"Failed to load script {type.FullName}: {e.Message}");
                    }
                }

                if (LoadedScripts.Count == 0)
                    LOGGER.Warn("No scripts found in external assembly.");
            }
            catch (Exception e)
            {
                LOGGER.Error($"Error loading external scripts: {e.Message}");
            }
        }

        private static void Tick(double deltaTime)
        {
            if (ServerTick >= 50)
                ServerTick = 0;
            else
                ServerTick++;

            foreach (var script in LoadedScripts)
            {
                try
                {
                    script.OnUpdate(ServerTick, AudioEngine);
                }
                catch (Exception e)
                {
                    LOGGER.Warn($"Script {script.ScriptId} OnUpdate failed: {e.Message}");
                }
            }
        }
        
        private static void PhysTick(double deltaTime)
        {
            if (ServerPhysTick >= 200)
                ServerPhysTick = 0;
            else
                ServerPhysTick++;
            
            RigidBodyComponent.SyncAllFromPhysics();
                
            if (PhysicsWorld.Initialized)
                PhysicsWorld.StepFixed(deltaTime);
        }

        public static void RenderFrame(RendererBase renderer)
        {
            foreach (var script in LoadedScripts)
            {
                try
                {
                    script.OnRenderFrame(renderer);
                }
                catch (Exception e)
                {
                    LOGGER.Warn($"Script {script.ScriptId} OnRenderFrame failed: {e.Message}");
                }
            }
        }

        public static void Shutdown()
        {
            LOGGER.Info("Shutting down Fulcrum Engine...");

            foreach (var script in LoadedScripts)
            {
                try
                {
                    script.OnUninstall();
                }
                catch (Exception e)
                {
                    LOGGER.Warn($"Script {script.ScriptId} OnUninstall failed: {e.Message}");
                }
            }
            LoadedScripts.Clear();

            EngineEvents.RaiseEngineShutdown();

            // 清理音频子引擎
            AudioEngine.Dispose();

            IsRun = false;
            MainTickThread.Interrupt();
        }
    }
}
