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

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using CMLS.CLogger;
using Veldrid;
using Veldrid.SPIRV;

namespace Fulcrum.Engine.Render.Shaders
{
	public sealed class ShaderProgram : IDisposable
	{
		public ShaderProgramKey Key { get; }
		public Shader[] Stages { get; private set; }

		private readonly Clogger _logger = LogManager.GetLogger("ShaderProgram");

		internal ShaderProgram(ShaderProgramKey key, Shader[] stages)
		{
			Key = key;
			Stages = stages ?? throw new ArgumentNullException(nameof(stages));
		}

		internal void ReplaceStages(Shader[] newStages)
		{
			var old = Stages;
			Stages = newStages ?? throw new ArgumentNullException(nameof(newStages));
			if (old != null)
			{
				foreach (var s in old) s?.Dispose();
			}
		}

		public void Dispose()
		{
			if (Stages != null)
			{
				foreach (var s in Stages)
				{
					try { s?.Dispose(); }
					catch (Exception ex)
					{
						_logger.Warn($"Dispose shader stage failed: {ex.Message}");
					}
				}
				Stages = null;
			}
		}
	}

	public readonly struct ShaderProgramKey : IEquatable<ShaderProgramKey>
	{
		public readonly string Name;
		public readonly string Variant;

		public ShaderProgramKey(string name, string? variant = null)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Variant = variant ?? "Default";
		}

		public override string ToString() => $"{Name}@{Variant}";

		public bool Equals(ShaderProgramKey other)
			=> string.Equals(Name, other.Name, StringComparison.Ordinal) &&
			   string.Equals(Variant, other.Variant, StringComparison.Ordinal);

		public override bool Equals(object? obj) => obj is ShaderProgramKey other && Equals(other);

		public override int GetHashCode()
		{
			var hc = new HashCode();
			hc.Add(Name, StringComparer.Ordinal);
			hc.Add(Variant, StringComparer.Ordinal);
			return hc.ToHashCode();
		}

		public static bool operator ==(ShaderProgramKey left, ShaderProgramKey right) => left.Equals(right);
		public static bool operator !=(ShaderProgramKey left, ShaderProgramKey right) => !left.Equals(right);
	}

	public sealed class ShaderPassDescriptor
	{
		/// <summary>要使用的 Program 标识</summary>
		public ShaderProgramKey ProgramKey { get; init; }

		/// <summary>顶点布局</summary>
		public VertexLayoutDescription VertexLayout { get; init; }

		/// <summary>图元拓扑（TriangleList / LineList 等）</summary>
		public PrimitiveTopology Topology { get; init; } = PrimitiveTopology.TriangleList;

		/// <summary>深度测试是否启用</summary>
		public bool DepthTestEnabled { get; init; } = true;

		/// <summary>是否写入深度缓冲</summary>
		public bool DepthWriteEnabled { get; init; } = true;

		/// <summary>剔除模式</summary>
		public FaceCullMode CullMode { get; init; } = FaceCullMode.Back;

		/// <summary>正面朝向</summary>
		public FrontFace FrontFace { get; init; } = FrontFace.Clockwise;

		/// <summary>混合状态</summary>
		public BlendStateDescription BlendStates { get; init; }
			= BlendStateDescription.SingleOverrideBlend;

		/// <summary>可选：额外的 ResourceLayout（Uniform / Texture 等）。顺序非常重要。</summary>
		public ResourceLayout[] ResourceLayouts { get; init; } = Array.Empty<ResourceLayout>();

		/// <summary>可选：输出描述；如果为 null 则使用 Swapchain 输出描述。</summary>
		public OutputDescription? OutputOverride { get; init; }

		public ShaderPassDescriptor()
		{
		}

		public static ShaderPassDescriptor CreateBasic(
			ShaderProgramKey key,
			VertexLayoutDescription vertexLayout,
			ResourceLayout[]? resourceLayouts = null,
			bool depthTest = true,
			bool depthWrite = true,
			PrimitiveTopology topology = PrimitiveTopology.TriangleList,
			FaceCullMode cull = FaceCullMode.Back,
			FrontFace frontFace = FrontFace.Clockwise,
			BlendStateDescription? blend = null)
		{
			return new ShaderPassDescriptor
			{
				ProgramKey = key,
				VertexLayout = vertexLayout,
				ResourceLayouts = resourceLayouts ?? Array.Empty<ResourceLayout>(),
				DepthTestEnabled = depthTest,
				DepthWriteEnabled = depthWrite,
				Topology = topology,
				CullMode = cull,
				FrontFace = frontFace,
				BlendStates = blend ?? BlendStateDescription.SingleOverrideBlend
			};
		}
	}

	public readonly struct ShaderPipelineKey : IEquatable<ShaderPipelineKey>
	{
		public readonly ShaderProgramKey ProgramKey;
		public readonly PrimitiveTopology Topology;
		public readonly FaceCullMode CullMode;
		public readonly FrontFace FrontFace;
		public readonly bool DepthTestEnabled;
		public readonly bool DepthWriteEnabled;
		public readonly BlendStateDescription Blend;
		public readonly int VertexLayoutHash;
		public readonly int ResourceLayoutHash;
		public readonly OutputDescription Output;

		public ShaderPipelineKey(
			ShaderProgramKey programKey,
			PrimitiveTopology topology,
			FaceCullMode cullMode,
			FrontFace frontFace,
			bool depthTest,
			bool depthWrite,
			BlendStateDescription blend,
			VertexLayoutDescription vertexLayout,
			IReadOnlyList<ResourceLayout> resourceLayouts,
			OutputDescription output)
		{
			ProgramKey = programKey;
			Topology = topology;
			CullMode = cullMode;
			FrontFace = frontFace;
			DepthTestEnabled = depthTest;
			DepthWriteEnabled = depthWrite;
			Blend = blend;
			Output = output;

			VertexLayoutHash = ComputeVertexLayoutHash(vertexLayout);
			ResourceLayoutHash = ComputeResourceLayoutHash(resourceLayouts);
		}

		private static int ComputeVertexLayoutHash(VertexLayoutDescription layout)
		{
			var hc = new HashCode();
			hc.Add(layout.InstanceStepRate);
			foreach (var e in layout.Elements)
			{
				hc.Add(e.Name, StringComparer.Ordinal);
				hc.Add((int)e.Semantic);
				hc.Add((int)e.Format);
			}
			return hc.ToHashCode();
		}

		private static int ComputeResourceLayoutHash(IReadOnlyList<ResourceLayout> layouts)
		{
			var hc = new HashCode();
			for (int i = 0; i < layouts.Count; i++)
			{
				hc.Add(RuntimeHelpers.GetHashCode(layouts[i]));
			}
			return hc.ToHashCode();
		}

		public bool Equals(ShaderPipelineKey other)
		{
			return ProgramKey.Equals(other.ProgramKey)
			       && Topology == other.Topology
			       && CullMode == other.CullMode
			       && FrontFace == other.FrontFace
			       && DepthTestEnabled == other.DepthTestEnabled
			       && DepthWriteEnabled == other.DepthWriteEnabled
			       && Blend.Equals(other.Blend)
			       && VertexLayoutHash == other.VertexLayoutHash
			       && ResourceLayoutHash == other.ResourceLayoutHash
			       && Output.Equals(other.Output);
		}

		public override bool Equals(object? obj) => obj is ShaderPipelineKey other && Equals(other);

		public override int GetHashCode()
		{
			var hc = new HashCode();
			hc.Add(ProgramKey);
			hc.Add((int)Topology);
			hc.Add((int)CullMode);
			hc.Add((int)FrontFace);
			hc.Add(DepthTestEnabled);
			hc.Add(DepthWriteEnabled);
			hc.Add(Blend);
			hc.Add(VertexLayoutHash);
			hc.Add(ResourceLayoutHash);
			hc.Add(Output);
			return hc.ToHashCode();
		}

		public static bool operator ==(ShaderPipelineKey left, ShaderPipelineKey right) => left.Equals(right);
		public static bool operator !=(ShaderPipelineKey left, ShaderPipelineKey right) => !left.Equals(right);
	}

	public sealed class ShaderFramework : IDisposable
	{
		private readonly GraphicsDevice _gd;
		private readonly ResourceFactory _factory;
		private readonly RendererConfig _config;

		private readonly Clogger _logger = LogManager.GetLogger("ShaderFramework");

		private readonly ConcurrentDictionary<ShaderProgramKey, ShaderProgram> _programs
			= new ConcurrentDictionary<ShaderProgramKey, ShaderProgram>();

		private readonly ConcurrentDictionary<ShaderPipelineKey, Pipeline> _pipelines
			= new ConcurrentDictionary<ShaderPipelineKey, Pipeline>();

		public ShaderFramework(GraphicsDevice gd, ResourceFactory factory, RendererConfig config)
		{
			_gd = gd ?? throw new ArgumentNullException(nameof(gd));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_config = config ?? throw new ArgumentNullException(nameof(config));

			_logger.Info($"ShaderFramework created. ShaderDir = '{_config.ShaderDirectory}'");
		}

		#region Program 注册 / 查询

		public ShaderProgram RegisterProgramFromFiles(
			string name,
			string? variant,
			string vertexRelPath,
			string fragmentRelPath)
		{
			var key = new ShaderProgramKey(name, variant);
			string vsPath = Path.Combine(_config.ShaderDirectory, vertexRelPath);
			string fsPath = Path.Combine(_config.ShaderDirectory, fragmentRelPath);

			if (!File.Exists(vsPath) || !File.Exists(fsPath))
			{
				throw new FileNotFoundException(
					$"Shader file not found. VS: {vsPath}  FS: {fsPath}");
			}

			var stages = CompileFromFiles(vsPath, fsPath);
			var program = new ShaderProgram(key, stages);

			_programs.AddOrUpdate(key, program, (_, old) =>
			{
				old.Dispose();
				return program;
			});

			InvalidatePipelinesForProgram(key);

			_logger.Info($"RegisterProgramFromFiles: {key} => VS='{vertexRelPath}', FS='{fragmentRelPath}'");
			return program;
		}

		public ShaderProgram RegisterProgramFromSpirvBytes(
			string name,
			string? variant,
			byte[] vertexBytes,
			byte[] fragmentBytes)
		{
			if (vertexBytes == null) throw new ArgumentNullException(nameof(vertexBytes));
			if (fragmentBytes == null) throw new ArgumentNullException(nameof(fragmentBytes));

			var key = new ShaderProgramKey(name, variant);
			var stages = CompileFromSpirvBytes(vertexBytes, fragmentBytes);
			var program = new ShaderProgram(key, stages);

			_programs.AddOrUpdate(key, program, (_, old) =>
			{
				old.Dispose();
				return program;
			});

			InvalidatePipelinesForProgram(key);
			_logger.Info($"RegisterProgramFromSpirvBytes: {key} (bytes)");
			return program;
		}

		public bool TryGetProgram(ShaderProgramKey key, out ShaderProgram program)
			=> _programs.TryGetValue(key, out program!);

		public void ReloadProgramFromFiles(
			string name,
			string? variant,
			string vertexRelPath,
			string fragmentRelPath)
		{
			var key = new ShaderProgramKey(name, variant);
			string vsPath = Path.Combine(_config.ShaderDirectory, vertexRelPath);
			string fsPath = Path.Combine(_config.ShaderDirectory, fragmentRelPath);

			if (!File.Exists(vsPath) || !File.Exists(fsPath))
			{
				throw new FileNotFoundException(
					$"Shader file not found for reload. VS: {vsPath}  FS: {fsPath}");
			}

			var stages = CompileFromFiles(vsPath, fsPath);

			if (_programs.TryGetValue(key, out var program))
			{
				program.ReplaceStages(stages);
				_logger.Info($"ReloadProgramFromFiles: {key} recompiled.");
			}
			else
			{
				_programs[key] = new ShaderProgram(key, stages);
				_logger.Info($"ReloadProgramFromFiles: {key} did not exist, created.");
			}

			InvalidatePipelinesForProgram(key);
		}

		#endregion

		#region Pipeline 获取 / 缓存

		public Pipeline GetOrCreatePipeline(ShaderPassDescriptor pass)
		{
			if (!_programs.TryGetValue(pass.ProgramKey, out var program))
			{
				throw new InvalidOperationException(
					$"ShaderProgram '{pass.ProgramKey}' not registered. " +
					"请先调用 ShaderFramework.RegisterProgramXXX。");
			}

			var output = pass.OutputOverride ?? _gd.SwapchainFramebuffer.OutputDescription;
			var layouts = pass.ResourceLayouts ?? Array.Empty<ResourceLayout>();

			var key = new ShaderPipelineKey(
				pass.ProgramKey,
				pass.Topology,
				pass.CullMode,
				pass.FrontFace,
				pass.DepthTestEnabled,
				pass.DepthWriteEnabled,
				pass.BlendStates,
				pass.VertexLayout,
				layouts,
				output);

			if (_pipelines.TryGetValue(key, out var cached))
			{
				return cached;
			}

			var desc = new GraphicsPipelineDescription
			{
				BlendState = pass.BlendStates,
				DepthStencilState = new DepthStencilStateDescription(
					pass.DepthTestEnabled,
					pass.DepthWriteEnabled,
					ComparisonKind.LessEqual),
				RasterizerState = new RasterizerStateDescription(
					pass.CullMode,
					fillMode: PolygonFillMode.Solid,
					pass.FrontFace,
					depthClipEnabled: true,
					scissorTestEnabled: false),
				PrimitiveTopology = pass.Topology,
				ResourceLayouts = layouts,
				ShaderSet = new ShaderSetDescription(
					new[] { pass.VertexLayout },
					program.Stages),
				Outputs = output
			};

			Pipeline pipeline;
			try
			{
				pipeline = _factory.CreateGraphicsPipeline(desc);
			}
			catch (Exception ex)
			{
				_logger.Error($"CreateGraphicsPipeline failed for {pass.ProgramKey}: {ex.Message}\n{ex.StackTrace}");
				throw;
			}

			_pipelines[key] = pipeline;
			return pipeline;
		}

		private void InvalidatePipelinesForProgram(ShaderProgramKey key)
		{
			var toRemove = new List<ShaderPipelineKey>();
			foreach (var kv in _pipelines)
			{
				if (kv.Key.ProgramKey.Equals(key))
				{
					toRemove.Add(kv.Key);
				}
			}

			foreach (var k in toRemove)
			{
				if (_pipelines.TryRemove(k, out var p))
				{
					try { p.Dispose(); }
					catch (Exception ex)
					{
						_logger.Warn($"Dispose old pipeline failed: {ex.Message}");
					}
				}
			}

			if (toRemove.Count > 0)
			{
				_logger.Info($"Invalidated {toRemove.Count} pipeline(s) for program {key}.");
			}
		}

		#endregion

		#region 编译辅助

		private Shader[] CompileFromFiles(string vsPath, string fsPath)
		{
			byte[] vsBytes = File.ReadAllBytes(vsPath);
			byte[] fsBytes = File.ReadAllBytes(fsPath);
			return CompileFromSpirvBytes(vsBytes, fsBytes);
		}

		private Shader[] CompileFromSpirvBytes(byte[] vsBytes, byte[] fsBytes)
		{
			try
			{
				return _factory.CreateFromSpirv(
					new ShaderDescription(ShaderStages.Vertex, vsBytes, "main"),
					new ShaderDescription(ShaderStages.Fragment, fsBytes, "main"));
			}
			catch (Exception ex)
			{
				_logger.Error($"CreateFromSpirv failed: {ex.Message}\n{ex.StackTrace}");
				throw;
			}
		}

		#endregion

		public void Dispose()
		{
			foreach (var kv in _pipelines)
			{
				try { kv.Value.Dispose(); }
				catch { /* ignore */ }
			}
			_pipelines.Clear();

			foreach (var kv in _programs)
			{
				try { kv.Value.Dispose(); }
				catch { /* ignore */ }
			}
			_programs.Clear();
		}
	}
}
