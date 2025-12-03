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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Fulcrum.Engine.Render.Shaders;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Fulcrum.Engine.Render
{
	public class RendererBase : IDisposable
	{
		private readonly Clogger LOGGER = LogManager.GetLogger("RenderBase");

		public GraphicsDevice _graphicsDevice;
		public RendererConfig _config;
		protected CommandList _commandList;
		public Swapchain _swapchain;

		public Sdl2Window _window;
		private readonly ConcurrentDictionary<string, RenderResource> _resources = new ConcurrentDictionary<string, RenderResource>();
		private readonly ConcurrentDictionary<string, IRenderable> _renderables = new ConcurrentDictionary<string, IRenderable>();
		
		public ResourceFactory ResourceFactory => _graphicsDevice.ResourceFactory;

		public IntPtr WindowHandle
		{
			get
			{
				if (_window == null)
					throw new InvalidOperationException("Window not initialized yet.");
				return _window.SdlWindowHandle;
			}
		}

		// 渲染队列
		private readonly List<IRenderable> _drawList = new List<IRenderable>();
		private readonly object _drawListLock = new object();

		private Camera _camera;
		public Camera Camera
		{
			get => _camera;
			set
			{
				_camera = value;
				UpdateAllRenderableMatrices();
			}
		}

		// 多线程渲染
		private int _workerCount;
		private Thread[] _workers;
		private CommandList[] _workerCmdLists;
		private ManualResetEventSlim[] _workBegin;
		private ManualResetEventSlim[] _workDone;
		private volatile bool _workersShouldExit = false;

		private struct WorkItem
		{
			public int Start;
			public int End;
			public bool DoClear;
			public Framebuffer FB;
			public RgbaFloat ClearColor;
			public bool HasDepth;
			public IRenderable[] Snapshot;
		}
		private WorkItem[] _workItems;

		private long _frameCount = 0;
		private DateTime _lastFrameTime = DateTime.Now;
		private readonly object _frameSync = new object();

		public Action<RendererBase> OnUpdate { get; set; }

		public Action<RendererBase> OnImGuiRender { get; set; }
		
		// 子引擎同步
		private EngineCoordinator _engineCoordinator;
		
		// API
		public string GraphicsBackendName => _graphicsDevice.BackendType.ToString();
		public int RenderThreadCount => _workItems.Length;
		public long GetRenderableCount => _renderables.Count;
		public long GetResourceCount => _resources.Count;

		// 帧率控制
		private int _targetFPS = 60;
		private readonly Stopwatch _frameTimer = new Stopwatch();
		private double _frameTimeMilliseconds = 16.666;
		private readonly object _fpsLock = new object();

		private readonly object _resourceLock = new object();

		private readonly ConcurrentDictionary<string, Pipeline> _pipelineCache = new ConcurrentDictionary<string, Pipeline>();

		// 全局着色器框架
		private readonly ShaderFramework _shaderFramework;
		public ShaderFramework ShaderFramework => _shaderFramework;

		// 键盘状态
		private readonly Dictionary<Key, bool> _keyStates = new Dictionary<Key, bool>();

		private readonly Stopwatch _frameStopwatch = new Stopwatch();
		private float _deltaTime = 0f;

		private bool DepthEnabled => _config.DepthFormat.HasValue;

		// ImGui 控制器
		private ImGuiRenderer _imguiController;
		public ImGuiRenderer ImGuiController => _imguiController;

		// 调试渲染
		private bool _debugSlowModeEnabled = false;
		private int _debugDelayPerDrawMs = 200;
		private bool _debugStepMode = false;
		private volatile bool _debugStepRequest = false;
		public bool DebugSlowModeEnabled => _debugSlowModeEnabled;
		public int DebugDelayPerDrawMs => _debugDelayPerDrawMs;
		public bool DebugStepMode => _debugStepMode;
		public int DebugCurrentIndex { get; private set; } = -1;
		public int DebugTotalCount { get; private set; } = 0;
		public string DebugCurrentRenderableName { get; private set; } = string.Empty;

		public RendererBase(RendererConfig config)
		{
			LOGGER.Debug($"RendererBase constructor called with config: {config.WindowWidth}x{config.WindowHeight}, Backend: {config.Backend}");

			_config = config;
			InitializeGraphicsDevice(config);
			
			_shaderFramework = new ShaderFramework(
				_graphicsDevice,
				_graphicsDevice.ResourceFactory,
				_config);

			_commandList = _graphicsDevice.ResourceFactory.CreateCommandList();

			_window.Closing += () => { LOGGER.Debug("Window closing event received"); };
			_window.Resized += () => OnWindowResized(_window.Width, _window.Height);
			
			_imguiController = new ImGuiRenderer(
				_graphicsDevice,
				_swapchain.Framebuffer.OutputDescription,
				_window.Width,
				_window.Height);
			
			_camera = new Camera
			{
				Position = new Vector3(0, 0, 5),
				Target = Vector3.Zero,
				AspectRatio = (float)config.WindowWidth / config.WindowHeight
			};

			foreach (Key key in Enum.GetValues(typeof(Key)))
				_keyStates[key] = false;

			_window.KeyDown += OnKeyDown;
			_window.KeyUp += OnKeyUp;

			_frameTimer.Start();
			_frameStopwatch.Start();
			
			_window.Closing += () =>
			{
				try { FulcrumEngine.Shutdown(); } catch {}
			};

			InitializeWorkers();

			LOGGER.Debug("RendererBase initialization completed");
		}

		#region 输入
		private void OnKeyDown(KeyEvent keyEvent) => _keyStates[keyEvent.Key] = true;
		private void OnKeyUp(KeyEvent keyEvent) => _keyStates[keyEvent.Key] = false;
		public bool IsKeyDown(Key key) => _keyStates.TryGetValue(key, out bool isDown) && isDown;
		#endregion

		#region 相机/矩阵
		private void UpdateAllRenderableMatrices()
		{
			if (_camera == null) return;
			var viewMatrix = _camera.GetViewMatrix();
			var projectionMatrix = _camera.GetProjectionMatrix();

			int updatedCount = 0;
			foreach (var renderable in _renderables.Values)
			{
				if (renderable is GeometryRenderable3D r3d)
				{
					r3d.ViewMatrix = viewMatrix;
					r3d.ProjectionMatrix = projectionMatrix;
					updatedCount++;
				}
			}
		}
		#endregion

		#region 窗口与设备
		public void OnWindowResized(int width, int height)
		{
			if (width <= 0 || height <= 0)
			{
				LOGGER.Warn($"OnWindowResized got invalid size: {width}x{height}, skip.");
				return;
			}
			_config.WindowWidth = width;
			_config.WindowHeight = height;

			try
			{
				_graphicsDevice.ResizeMainWindow((uint)width, (uint)height);
				_swapchain = _graphicsDevice.MainSwapchain;
			}
			catch (Exception ex)
			{
				LOGGER.Error($"ResizeMainWindow failed: {ex.Message}\n{ex.StackTrace}");
			}

			_imguiController?.WindowResized(width, height);

			if (_camera != null)
			{
				_camera.AspectRatio = (float)width / height;
				UpdateAllRenderableMatrices();
			}
		}

		private void InitializeGraphicsDevice(RendererConfig config)
		{
			var windowCI = new WindowCreateInfo
			{
				X = 100,
				Y = 100,
				WindowWidth = config.WindowWidth,
				WindowHeight = config.WindowHeight,
				WindowTitle = config.WindowTitle
			};

			var options = new GraphicsDeviceOptions
			{
				PreferStandardClipSpaceYDirection = true,
				PreferDepthRangeZeroToOne = true,
				Debug = _config.EnableValidation,
				SwapchainDepthFormat = _config.DepthFormat,
				SyncToVerticalBlank = false
			};

			_window = VeldridStartup.CreateWindow(ref windowCI);
			_graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options, _config.Backend);
			_swapchain = _graphicsDevice.MainSwapchain;
		}
		#endregion

		#region FPS
		public void SetFPS(int fps)
		{
			if (fps <= 0) throw new ArgumentException("FPS must be greater than 0");
			lock (_fpsLock)
			{
				_targetFPS = fps;
				_frameTimeMilliseconds = 1000.0 / fps;
			}
		}
		public double GetCurrentFPS()
		{
			lock (_frameSync)
			{
				if (_frameCount == 0) return 0;
				return _frameCount / (DateTime.Now - _lastFrameTime).TotalSeconds;
			}
		}
		public int GetTargetFPS() { lock (_fpsLock) return _targetFPS; }
		#endregion

		#region 资源 & Renderable
		public void AddResource(RenderResource resource)
		{
			resource.Initialize(_graphicsDevice, _graphicsDevice.ResourceFactory);
			_resources[resource.Name] = resource;
		}
		public T GetResource<T>(string name) where T : RenderResource
			=> _resources.TryGetValue(name, out var r) ? r as T : throw new KeyNotFoundException($"Resource '{name}' not found");

		public void AddRenderable(IRenderable renderable)
		{
			lock (_resourceLock)
			{
				renderable.Initialize(_graphicsDevice, _graphicsDevice.ResourceFactory);
				_renderables[renderable.Name] = renderable;
				
				if (_camera != null && renderable is GeometryRenderable3D r3d)
				{
					var view = _camera.GetViewMatrix();
					var proj = _camera.GetProjectionMatrix();
					r3d.ViewMatrix = view;
					r3d.ProjectionMatrix = proj;
				}

				lock (_drawListLock)
				{
					_drawList.Add(renderable);
				}
			}
		}

		public T GetRenderable<T>(string name) where T : IRenderable
			=> _renderables.TryGetValue(name, out var r) ? (T)r : throw new KeyNotFoundException($"Renderable '{name}' not found");

		public void RemoveRenderable(string name)
		{
			IRenderable renderable = null;

			lock (_resourceLock)
			{
				_renderables.TryRemove(name, out renderable);
			}

			if (renderable != null)
			{
				lock (_drawListLock)
				{
					for (int i = _drawList.Count - 1; i >= 0; i--)
					{
						if (_drawList[i].Name == name)
						{
							_drawList.RemoveAt(i);
							break;
						}
					}
				}

				LOGGER.Debug($"Renderable '{name}' removed from renderer (not disposed automatically).");
			}
		}
		#endregion

		#region 工人线程池
		private void InitializeWorkers(int? wantCount = null)
		{
			var logical = Math.Max(2, Environment.ProcessorCount);
			_workerCount = Math.Clamp(wantCount ?? (logical - 1), 1, 8);

			LOGGER.Info($"Initializing render workers: {_workerCount} threads");

			_workers = new Thread[_workerCount];
			_workerCmdLists = new CommandList[_workerCount];
			_workBegin = new ManualResetEventSlim[_workerCount];
			_workDone = new ManualResetEventSlim[_workerCount];
			_workItems = new WorkItem[_workerCount];

			for (int i = 0; i < _workerCount; i++)
			{
				_workerCmdLists[i] = _graphicsDevice.ResourceFactory.CreateCommandList();
				_workBegin[i] = new ManualResetEventSlim(false);
				_workDone[i] = new ManualResetEventSlim(false);

				int id = i;
				_workers[i] = new Thread(() => WorkerLoop(id))
				{
					IsBackground = true,
					Name = $"RenderWorker-{id}"
				};
				_workers[i].Start();
			}
		}

		private void WorkerLoop(int id)
		{
			var cmd = _workerCmdLists[id];
			while (true)
			{
				_workBegin[id].Wait();
				_workBegin[id].Reset();

				if (_workersShouldExit) break;

				try
				{
					var job = _workItems[id];

					cmd.Begin();
					cmd.SetFramebuffer(job.FB);

					if (job.DoClear)
					{
						cmd.ClearColorTarget(0, job.ClearColor);
						if (job.HasDepth) cmd.ClearDepthStencil(1f);
					}

					if (job.Snapshot != null && job.End > job.Start)
					{
						for (int i = job.Start; i < job.End; i++)
						{
							job.Snapshot[i]?.Draw(cmd);
						}
					}

					cmd.End();
				}
				catch (Exception ex)
				{
					LOGGER.Error($"Worker {id} record failed: {ex.Message}\n{ex.StackTrace}");
				}
				finally
				{
					_workDone[id].Set();
				}
			}
		}

		public void SetRenderThreadCount(int count)
		{
			count = Math.Clamp(count, 1, 8);
			if (count == _workerCount) return;

			ShutdownWorkers();

			InitializeWorkers(count);
		}

		private void ShutdownWorkers()
		{
			if (_workers == null) return;
			_workersShouldExit = true;
			for (int i = 0; i < _workerCount; i++)
				_workBegin[i].Set();

			for (int i = 0; i < _workerCount; i++)
			{
				try { _workers[i]?.Join(500); } catch { }
			}

			for (int i = 0; i < _workerCount; i++)
			{
				try { _workerCmdLists[i]?.Dispose(); } catch { }
				try { _workBegin[i]?.Dispose(); } catch { }
				try { _workDone[i]?.Dispose(); } catch { }
			}

			_workers = null;
			_workerCmdLists = null;
			_workBegin = null;
			_workDone = null;
			_workItems = null;
			_workersShouldExit = false;
		}
		#endregion

		#region 调试渲染
		public void EnableSlowDebugMode(bool enabled, int delayPerDrawMs = 200, bool stepMode = false)
		{
			_debugSlowModeEnabled = enabled;
			_debugDelayPerDrawMs = Math.Max(0, delayPerDrawMs);
			_debugStepMode = stepMode;
			_debugStepRequest = false;
			DebugCurrentIndex = -1;
			DebugTotalCount = 0;
			DebugCurrentRenderableName = string.Empty;
			LOGGER.Info($"Slow debug mode: {(enabled ? "ON" : "OFF")}, delay={_debugDelayPerDrawMs}ms, stepMode={_debugStepMode}");
		}
		
		public void DebugStepOnce()
		{
			if (!_debugSlowModeEnabled || !_debugStepMode) return;
			_debugStepRequest = true;
		}

		private void RenderFrameSlowDebug(IRenderable[] snapshot, Framebuffer fb)
		{
			DebugTotalCount = snapshot.Length;

			if (snapshot.Length == 0)
			{
				_commandList.Begin();
				_commandList.SetFramebuffer(fb);
				_commandList.ClearColorTarget(0, _config.ClearColor);
				if (DepthEnabled) _commandList.ClearDepthStencil(1f);

				if (_imguiController != null)
				{
					_imguiController.Render(_graphicsDevice, _commandList);
				}

				_commandList.End();
				_graphicsDevice.SubmitCommands(_commandList);
				_graphicsDevice.SwapBuffers();
				return;
			}

			for (int i = 0; i < snapshot.Length; i++)
			{
				if (_debugStepMode)
				{
					while (!_debugStepRequest)
					{
						if (!_window.Exists) return;
						Thread.Sleep(1);
					}
					_debugStepRequest = false;
				}

				DebugCurrentIndex = i;
				var renderable = snapshot[i];
				DebugCurrentRenderableName = renderable?.Name ?? string.Empty;

				_commandList.Begin();
				_commandList.SetFramebuffer(fb);

				if (i == 0)
				{
					_commandList.ClearColorTarget(0, _config.ClearColor);
					if (DepthEnabled) _commandList.ClearDepthStencil(1f);
				}

				try
				{
					renderable?.Draw(_commandList);
				}
				catch (Exception ex)
				{
					LOGGER.Error($"SlowDebug draw [{i}] '{DebugCurrentRenderableName}' failed: {ex.Message}\n{ex.StackTrace}");
				}

				if (_imguiController != null)
				{
					_imguiController.Render(_graphicsDevice, _commandList);
				}

				_commandList.End();
				_graphicsDevice.SubmitCommands(_commandList);
				_graphicsDevice.SwapBuffers();

				if (!_debugStepMode && _debugDelayPerDrawMs > 0)
				{
					Thread.Sleep(_debugDelayPerDrawMs);
				}
			}
		}
		#endregion

		#region 主循环
		public void Run()
		{
			bool windowExists = true;

			LOGGER.Info("Renderer main loop started");
			while (windowExists)
			{
				_deltaTime = (float)_frameStopwatch.Elapsed.TotalSeconds;
				_frameStopwatch.Restart();

				var snapshot = _window.PumpEvents();
				windowExists = _window.Exists;

				if (!windowExists) break;

				OnUpdate?.Invoke(this);

				_imguiController?.Update(_deltaTime, snapshot);

				OnImGuiRender?.Invoke(this);

				RenderFrame();
				ControlFrameRate();
			}

			LOGGER.Debug("Renderer main loop ended");
			Dispose();
		}

		public float GetDeltaTime() => _deltaTime;

		private void ControlFrameRate()
		{
			double elapsed = _frameTimer.Elapsed.TotalMilliseconds;
			double target;
			lock (_fpsLock) target = _frameTimeMilliseconds;

			if (elapsed < target)
			{
				int sleepMs = (int)(target - elapsed);
				if (sleepMs > 0) Thread.Sleep(sleepMs);
			}
			_frameTimer.Restart();
		}

		private void RenderFrame()
		{
			if (_swapchain == null || _swapchain.Framebuffer == null)
			{
				LOGGER.Error("Swapchain or Framebuffer is null, skipping frame");
				return;
			}
			
			if (_camera != null)
			{
				UpdateAllRenderableMatrices();
			}

			try
			{
				IRenderable[] snapshot;
				lock (_drawListLock)
				{
					snapshot = _drawList.Count == 0
						? Array.Empty<IRenderable>()
						: _drawList.ToArray();
				}

				var fb = _swapchain.Framebuffer;
				var rendered = false;

				if (snapshot.Length == 0 && !_debugSlowModeEnabled)
				{
					RenderFrameEmpty(fb);
					rendered = true;
				}
				else if (_debugSlowModeEnabled)
				{
					RenderFrameSlowDebug(snapshot, fb);
					rendered = true;
				}
				else
				{
					const int MultiThreadThreshold = 32;

					if (snapshot.Length >= MultiThreadThreshold)
					{
						RenderFrameMultithreaded(snapshot, fb);
					}
					else
					{
						RenderFrameSingleThread(snapshot, fb);
					}

					rendered = true;
				}

				if (rendered)
				{
					Interlocked.Increment(ref _frameCount);
					var now = DateTime.Now;
					if ((now - _lastFrameTime).TotalSeconds >= 1.0)
					{
						lock (_frameSync)
						{
							_frameCount = 0;
							_lastFrameTime = now;
						}
					}
				}
			}
			catch (Exception ex)
			{
				LOGGER.Error($"Render error: {ex.Message}\n{ex.StackTrace}");
			}
		}
		
		private void RenderFrameEmpty(Framebuffer fb)
		{
			_commandList.Begin();
			_commandList.SetFramebuffer(fb);
			_commandList.ClearColorTarget(0, _config.ClearColor);
			if (DepthEnabled)
			{
				_commandList.ClearDepthStencil(1f);
			}

			if (_imguiController != null)
			{
				_imguiController.Render(_graphicsDevice, _commandList);
			}

			_commandList.End();
			_graphicsDevice.SubmitCommands(_commandList);
			_graphicsDevice.SwapBuffers();
		}
		
		private void RenderFrameSingleThread(IRenderable[] snapshot, Framebuffer fb)
		{
			_commandList.Begin();
			_commandList.SetFramebuffer(fb);
			_commandList.ClearColorTarget(0, _config.ClearColor);
			if (DepthEnabled)
			{
				_commandList.ClearDepthStencil(1f);
			}

			for (int i = 0; i < snapshot.Length; i++)
			{
				snapshot[i]?.Draw(_commandList);
			}

			if (_imguiController != null)
			{
				_imguiController.Render(_graphicsDevice, _commandList);
			}

			_commandList.End();
			_graphicsDevice.SubmitCommands(_commandList);
			_graphicsDevice.SwapBuffers();
		}
		
		private void RenderFrameMultithreaded(IRenderable[] snapshot, Framebuffer fb)
		{
		    int logicalCores = Environment.ProcessorCount;
		    int workerCount = Math.Min(logicalCores, snapshot.Length);
		    if (workerCount <= 1)
		    {
		        RenderFrameSingleThread(snapshot, fb);
		        return;
		    }

		    int sliceSize = (snapshot.Length + workerCount - 1) / workerCount;

		    var tasks = new List<Task>(workerCount);
		    var workerCmdLists = new List<CommandList>(workerCount);

		    for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
		    {
		        int start = workerIndex * sliceSize;
		        int end = Math.Min(snapshot.Length, start + sliceSize);

		        if (start >= end)
		        {
		            break;
		        }

		        CommandList cmdList = _graphicsDevice.ResourceFactory.CreateCommandList();
		        workerCmdLists.Add(cmdList);

		        int taskStart = start;
		        int taskEnd = end;

		        tasks.Add(Task.Run(() =>
		        {
		            try
		            {
		                cmdList.Begin();
		                cmdList.SetFramebuffer(fb);

		                for (int i = taskStart; i < taskEnd; i++)
		                {
		                    snapshot[i]?.Draw(cmdList);
		                }

		                cmdList.End();
		            }
		            catch (Exception ex)
		            {
		                LOGGER.Error($"Worker {workerIndex} record failed: {ex.Message}\n{ex.StackTrace}");
		                try { cmdList.Dispose(); } catch { /* ignore */ }
		                throw;
		            }
		        }));
		    }

		    try
		    {
		        Task.WaitAll(tasks.ToArray());
		    }
		    catch (AggregateException ae)
		    {
		        LOGGER.Error("Multithreaded render record failed: " + ae.Flatten().Message);
		        foreach (var cl in workerCmdLists)
		        {
		            try { cl.Dispose(); } catch { /* ignore */ }
		        }
		        return;
		    }

		    _commandList.Begin();
		    _commandList.SetFramebuffer(fb);
		    _commandList.ClearColorTarget(0, _config.ClearColor);
		    if (DepthEnabled)
		    {
		        _commandList.ClearDepthStencil(1f);
		    }
		    _commandList.End();
		    _graphicsDevice.SubmitCommands(_commandList);

		    foreach (var cmdList in workerCmdLists)
		    {
		        _graphicsDevice.SubmitCommands(cmdList);
		        cmdList.Dispose();
		    }

		    if (_imguiController != null)
		    {
		        _commandList.Begin();
		        _commandList.SetFramebuffer(fb);
		        _imguiController.Render(_graphicsDevice, _commandList);
		        _commandList.End();
		        _graphicsDevice.SubmitCommands(_commandList);
		    }
		    _graphicsDevice.SwapBuffers();
		}

		#endregion

		#region 其他

		public void SetEngineCoordinator(EngineCoordinator coordinator)
		{
			_engineCoordinator = coordinator;
		}
		
		public EngineCoordinator GetEngineCoordinator() => _engineCoordinator;
		
		#endregion

		#region 退出与释放
		public void RequestExit()
		{
			try { _window?.Close(); } catch { }
		}

		public virtual void Dispose()
		{
			LOGGER.Debug("Dispose() method called");

			ShutdownWorkers();

			lock (_resourceLock)
			{
				foreach (var resource in _resources.Values)
				{
					try { resource.Dispose(); } catch (Exception ex) { LOGGER.Warn($"Dispose resource '{resource.Name}' failed: {ex.Message}"); }
				}
				_resources.Clear();

				foreach (var renderable in _renderables.Values)
				{
					try { renderable.Dispose(); } catch (Exception ex) { LOGGER.Warn($"Dispose renderable '{renderable.Name}' failed: {ex.Message}"); }
				}
				_renderables.Clear();
			}

			lock (_drawListLock)
			{
				_drawList.Clear();
			}

			if (_window != null)
			{
				_window.KeyDown -= OnKeyDown;
				_window.KeyUp -= OnKeyUp;
			}

			try { _commandList?.Dispose(); } catch { }

			foreach (var p in _pipelineCache.Values)
			{
				try { p?.Dispose(); } catch { }
			}
			_pipelineCache.Clear();
			
			try { _shaderFramework.Dispose(); } catch { }

			try { _imguiController?.Dispose(); } catch { }

			try { _graphicsDevice?.Dispose(); } catch { }
			
			_engineCoordinator?.Dispose();
			_engineCoordinator = null;
			
			_frameTimer.Stop();
			LOGGER.Info("Renderer disposed successfully");
		}
		#endregion
	}
}
