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
using Fulcrum.Common;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Sdl2;
using Veldrid.SPIRV;
using Veldrid.StartupUtilities;

namespace Fulcrum.Engine.Render.Utils
{
	// 渲染器配置类
	public class RendererConfig
	{
		public GraphicsBackend Backend { get; set; } = GraphicsBackend.Vulkan;
		public string WindowTitle { get; set; } = "Veldrid Renderer";
		public int WindowWidth { get; set; } = 800;
		public int WindowHeight { get; set; } = 600;
		public bool EnableValidation { get; set; } = true;
		public string ShaderDirectory { get; set; } = "shaders";
		public PixelFormat? DepthFormat { get; set; } = PixelFormat.D24_UNorm_S8_UInt;
		public RgbaFloat ClearColor { get; set; } = RgbaFloat.Black;
	}

	// 可渲染对象接口
	public interface IRenderable : IDisposable
	{
		void Initialize(GraphicsDevice gd, ResourceFactory factory);
		void Draw(CommandList commandList);
		string Name { get; }
	}

	// 渲染资源基类
	public abstract class RenderResource : IDisposable
	{
		public string Name { get; protected set; }
		public abstract void Initialize(GraphicsDevice gd, ResourceFactory factory);
		public abstract void Dispose();
	}

	// 基础几何体渲染器
	public class GeometryRenderable : IRenderable
	{
		protected GraphicsDevice _graphicsDevice;
		protected DeviceBuffer _vertexBuffer;
		protected DeviceBuffer _indexBuffer;
		protected Pipeline _pipeline;
		protected Shader[] _shaders;
		protected RendererConfig _config;

		private Clogger LOGGER = LogManager.GetLogger("Render");

		public string Name { get; private set; }
		public VertexLayoutDescription VertexLayout { get; private set; }
		public GraphicsPipelineDescription PipelineDescription { get; set; }

		public string VertexShaderPath { get; set; }
		public string FragmentShaderPath { get; set; }
		public byte[] VertexShaderBytes { get; set; }
		public byte[] FragmentShaderBytes { get; set; }

		public GeometryRenderable(string name, VertexLayoutDescription vertexLayout, GraphicsPipelineDescription pipelineDescription)
		{
			Name = name;
			VertexLayout = vertexLayout;
			PipelineDescription = pipelineDescription;
		}

		public virtual void Initialize(GraphicsDevice gd, ResourceFactory factory)
		{
			_graphicsDevice = gd;
			if (gd is GraphicsDevice graphicsDevice && graphicsDevice is Veldrid.GraphicsDevice veldridDevice)
			{
			}
			// 创建着色器
			CreateShaders(factory);
			// 创建管道
			CreatePipeline(factory);
		}

		public void SetConfig(RendererConfig config)
		{
			_config = config;
		}

		protected virtual void CreateShaders(ResourceFactory factory)
		{
			if (VertexShaderBytes != null && FragmentShaderBytes != null)
			{
				_shaders = factory.CreateFromSpirv(
					new ShaderDescription(ShaderStages.Vertex, VertexShaderBytes, "main"),
					new ShaderDescription(ShaderStages.Fragment, FragmentShaderBytes, "main")
				);
			}
			else if (!string.IsNullOrEmpty(VertexShaderPath) && !string.IsNullOrEmpty(FragmentShaderPath))
			{
				string vertexShaderFullPath = Path.Combine(Global.GamePath, VertexShaderPath);
				string fragmentShaderFullPath = Path.Combine(Global.GamePath, FragmentShaderPath);

				if (File.Exists(vertexShaderFullPath) && File.Exists(fragmentShaderFullPath))
				{
					_shaders = factory.CreateFromSpirv(
						new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes(vertexShaderFullPath), "main"),
						new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes(fragmentShaderFullPath), "main")
					);
				}
				else
				{
					throw new FileNotFoundException($"Shader files not found: {vertexShaderFullPath} or {fragmentShaderFullPath}");
				}
			}
			else
			{
				throw new InvalidOperationException("No shader source provided. Set either shader paths or shader bytes.");
			}
		}

		protected virtual void CreatePipeline(ResourceFactory factory)
		{
			if (_shaders == null)
			{
				LOGGER.Warn("着色器未初始化，跳过管道创建");
				return;
			}

			var pipelineDesc = PipelineDescription;
			pipelineDesc.ShaderSet = new ShaderSetDescription(
				new[] { VertexLayout },
				_shaders
			);

			if (_graphicsDevice?.SwapchainFramebuffer == null)
			{
				LOGGER.Error("图形设备或交换链帧缓冲区未初始化");
				return;
			}
			pipelineDesc.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;

			if (pipelineDesc.ResourceLayouts == null)
			{
				pipelineDesc.ResourceLayouts = Array.Empty<ResourceLayout>();
			}

			PipelineDescription = pipelineDesc;

			try
			{
				_pipeline = factory.CreateGraphicsPipeline(PipelineDescription);
			}
			catch (NullReferenceException ex)
			{
				LOGGER.Error($"创建管道时发生空引用异常: {ex.Message}");
			}
			catch (Exception ex)
			{
				LOGGER.Error($"创建图形管道失败: {ex.Message}");
			}
		}

		// 设置顶点数据
		public virtual void SetVertexData<T>(T[] vertices) where T : unmanaged
		{
			if (_graphicsDevice == null)
			{
				throw new InvalidOperationException("Graphics device not initialized. Call Initialize() first.");
			}

			ArgumentNullException.ThrowIfNull(vertices);

			_vertexBuffer?.Dispose();
			_vertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(
				new BufferDescription((uint)(vertices.Length * SizeOf<T>()), BufferUsage.VertexBuffer));
			_graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices);
		}

		// 设置索引数据
		public virtual void SetIndexData<T>(T[] indices) where T : unmanaged
		{
			_indexBuffer?.Dispose();
			_indexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(
				new BufferDescription((uint)(indices.Length * SizeOf<T>()), BufferUsage.IndexBuffer));
			_graphicsDevice.UpdateBuffer(_indexBuffer, 0, indices);
		}

		// 辅助方法：获取类型大小
		public static uint SizeOf<T>() where T : unmanaged
		{
			return (uint)Marshal.SizeOf<T>();
		}

		// 设置着色器
		public virtual void SetShaders(Shader[] shaders)
		{
			_shaders = shaders;
			// 重新创建管道
			CreatePipeline(_graphicsDevice.ResourceFactory);
		}

		public virtual void SetShaderBytes(byte[] vertexShaderBytes, byte[] fragmentShaderBytes)
		{
			VertexShaderBytes = vertexShaderBytes;
			FragmentShaderBytes = fragmentShaderBytes;

			// 如果已经初始化，重新创建着色器和管道
			if (_graphicsDevice != null)
			{
				CreateShaders(_graphicsDevice.ResourceFactory);
				CreatePipeline(_graphicsDevice.ResourceFactory);
			}
		}

		public virtual void SetShaderPaths(string vertexShaderPath, string fragmentShaderPath)
		{
			VertexShaderPath = vertexShaderPath;
			FragmentShaderPath = fragmentShaderPath;

			// 如果已经初始化，重新创建着色器和管道
			if (_graphicsDevice != null)
			{
				CreateShaders(_graphicsDevice.ResourceFactory);
				CreatePipeline(_graphicsDevice.ResourceFactory);
			}
		}

		public virtual void Draw(CommandList commandList)
		{
			if (_vertexBuffer == null || _pipeline == null) return;

			commandList.SetVertexBuffer(0, _vertexBuffer);
			commandList.SetPipeline(_pipeline);

			if (_indexBuffer != null)
			{
				commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
				commandList.DrawIndexed(
					indexCount: (uint)(_indexBuffer.SizeInBytes / sizeof(ushort)),
					instanceCount: 1,
					indexStart: 0,
					vertexOffset: 0,
					instanceStart: 0);
			}
			else
			{
				commandList.Draw((uint)(_vertexBuffer.SizeInBytes / VertexLayout.Elements.Sum(e => e.Format.GetSizeInBytes())));
			}
		}

		public virtual void Dispose()
		{
			_vertexBuffer?.Dispose();
			_indexBuffer?.Dispose();
			_pipeline?.Dispose();

			if (_shaders != null)
			{
				foreach (var shader in _shaders)
				{
					shader?.Dispose();
				}
			}
		}
	}

	// 纹理渲染器
	public class TexturedRenderable : GeometryRenderable
	{
		private ResourceSet _resourceSet;
		private TextureView _textureView;
		private ResourceLayout _resourceLayout;

		public TexturedRenderable(string name, TextureView textureView, VertexLayoutDescription vertexLayout, GraphicsPipelineDescription pipelineDescription)
			: base(name, vertexLayout, pipelineDescription)
		{
			_textureView = textureView;
		}

		protected override void CreatePipeline(ResourceFactory factory)
		{
			// 创建资源布局
			_resourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
					new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)
				)
			);

			// 创建资源集
			_resourceSet = factory.CreateResourceSet(
				new ResourceSetDescription(
					_resourceLayout,
					_textureView,
					_graphicsDevice.PointSampler
				)
			);

			// 设置管道描述
			var pipelineDesc = PipelineDescription;
			pipelineDesc.ResourceLayouts = new[] { _resourceLayout };
			PipelineDescription = pipelineDesc;

			base.CreatePipeline(factory);
		}

		public override void Draw(CommandList commandList)
		{
			if (_vertexBuffer == null || _pipeline == null) return;

			commandList.SetVertexBuffer(0, _vertexBuffer);
			commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
			commandList.SetPipeline(_pipeline);
			commandList.SetGraphicsResourceSet(0, _resourceSet);
			commandList.DrawIndexed(6, 1, 0, 0, 0);
		}

		public override void Dispose()
		{
			base.Dispose();
			_resourceSet?.Dispose();
			_resourceLayout?.Dispose();
		}
	}

	public static class VertexElementFormatExtensions
	{
		public static uint GetSizeInBytes(this VertexElementFormat format)
		{
			switch (format)
			{
				case VertexElementFormat.Float1: return 4;
				case VertexElementFormat.Float2: return 8;
				case VertexElementFormat.Float3: return 12;
				case VertexElementFormat.Float4: return 16;
				case VertexElementFormat.Byte2_Norm: return 2;
				case VertexElementFormat.Byte2: return 2;
				case VertexElementFormat.Byte4_Norm: return 4;
				case VertexElementFormat.Byte4: return 4;
				case VertexElementFormat.SByte2_Norm: return 2;
				case VertexElementFormat.SByte2: return 2;
				case VertexElementFormat.SByte4_Norm: return 4;
				case VertexElementFormat.SByte4: return 4;
				case VertexElementFormat.Short2_Norm: return 4;
				case VertexElementFormat.Short2: return 4;
				case VertexElementFormat.Short4_Norm: return 8;
				case VertexElementFormat.Short4: return 8;
				case VertexElementFormat.UShort2_Norm: return 4;
				case VertexElementFormat.UShort2: return 4;
				case VertexElementFormat.UShort4_Norm: return 8;
				case VertexElementFormat.UShort4: return 8;
				case VertexElementFormat.UInt1: return 4;
				case VertexElementFormat.UInt2: return 8;
				case VertexElementFormat.UInt3: return 12;
				case VertexElementFormat.UInt4: return 16;
				case VertexElementFormat.Int1: return 4;
				case VertexElementFormat.Int2: return 8;
				case VertexElementFormat.Int3: return 12;
				case VertexElementFormat.Int4: return 16;
				case VertexElementFormat.Half1: return 2;
				case VertexElementFormat.Half2: return 4;
				case VertexElementFormat.Half4: return 8;
				default: throw new ArgumentException($"Unsupported format: {format}");
			}
		}

		// 3D顶点结构体
		public struct VertexPositionNormalTexture
		{
			public const uint SizeInBytes = 32;
			public Vector3 Position;
			public Vector3 Normal;
			public Vector2 TexCoord;

			public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 texCoord)
			{
				Position = position;
				Normal = normal;
				TexCoord = texCoord;
			}

			public static VertexLayoutDescription Layout => new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
		}

		// 3D变换矩阵结构
		public struct Matrix4x4Uniform
		{
			public Matrix4x4 Model;
			public Matrix4x4 View;
			public Matrix4x4 Projection;

			public static uint SizeInBytes => (uint)Marshal.SizeOf<Matrix4x4Uniform>();
		}

		// Uniform缓冲区资源
		public class UniformBufferResource<T> : RenderResource where T : unmanaged
		{
			private DeviceBuffer _buffer;

			public UniformBufferResource(string name)
			{
				Name = name;
			}

			public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
			{
				_buffer = factory.CreateBuffer(new BufferDescription(SizeOf<T>(), BufferUsage.UniformBuffer));
			}

			public void Update(GraphicsDevice gd, ref T data)
			{
				gd.UpdateBuffer(_buffer, 0, ref data);
			}

			public DeviceBuffer GetBuffer() => _buffer;

			public override void Dispose()
			{
				_buffer?.Dispose();
			}

			private static uint SizeOf<T>() where T : unmanaged
			{
				return (uint)Marshal.SizeOf<T>();
			}
		}

		// 3D几何体渲染器
		public class GeometryRenderable3D : GeometryRenderable
		{
			protected ResourceSet _resourceSet;
			protected ResourceLayout _resourceLayout;
			protected UniformBufferResource<Matrix4x4Uniform> _matrixUniform;

			private object _pendingVertices;
			private Type _pendingVerticesType;

			public Matrix4x4 ModelMatrix { get; set; } = Matrix4x4.Identity;
			public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
			public Matrix4x4 ProjectionMatrix { get; set; } = Matrix4x4.Identity;

			public GeometryRenderable3D(string name, VertexLayoutDescription vertexLayout, GraphicsPipelineDescription pipelineDescription)
				: base(name, vertexLayout, pipelineDescription)
			{
			}

			public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
			{
				_graphicsDevice = gd;

				// 初始化 Uniform 缓冲区
				_matrixUniform = new UniformBufferResource<Matrix4x4Uniform>($"{Name}_MatrixUniform");
				_matrixUniform.Initialize(gd, factory);

				// 创建资源布局
				_resourceLayout = factory.CreateResourceLayout(
					new ResourceLayoutDescription(
						new ResourceLayoutElementDescription("WorldViewProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex)
					)
				);

				// 创建资源集
				_resourceSet = factory.CreateResourceSet(
					new ResourceSetDescription(
						_resourceLayout,
						_matrixUniform.GetBuffer()
					)
				);

				// 更新管道描述以包含资源布局
				var pipelineDesc = PipelineDescription;
				var existingLayouts = pipelineDesc.ResourceLayouts?.ToList() ?? new List<ResourceLayout>();
				existingLayouts.Add(_resourceLayout);
				pipelineDesc.ResourceLayouts = existingLayouts.ToArray();
				PipelineDescription = pipelineDesc;

				if (_pendingVertices != null)
				{
					var method = typeof(GeometryRenderable)
						.GetMethod("CreateVertexBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
					var genericMethod = method.MakeGenericMethod(_pendingVerticesType);
					genericMethod.Invoke(this, new object[] { _pendingVertices });

					_pendingVertices = null;
					_pendingVerticesType = null;
				}

				// 创建着色器和管道
				CreateShaders(factory);
				CreatePipeline(factory);
				
				// === Lighting UBO 初始化 ===
				_lightingUniform = new UniformBufferResource<LightingParams>($"{Name}_LightingUniform");
				_lightingUniform.Initialize(gd, factory);

				_lightingLayout = factory.CreateResourceLayout(
					new ResourceLayoutDescription(
						new ResourceLayoutElementDescription("Lighting", ResourceKind.UniformBuffer, ShaderStages.Fragment)
					)
				);

				_lightingSet = factory.CreateResourceSet(
					new ResourceSetDescription(_lightingLayout, _lightingUniform.GetBuffer())
				);

				// 把 layout 追加到管线布局数组（矩阵layout 已经在前）
				var pipelineDesc2 = PipelineDescription;
				var layouts = pipelineDesc2.ResourceLayouts?.ToList() ?? new List<ResourceLayout>();
				if (!layouts.Contains(_resourceLayout)) layouts.Add(_resourceLayout);
				layouts.Add(_lightingLayout);
				pipelineDesc2.ResourceLayouts = layouts.ToArray();
				PipelineDescription = pipelineDesc2;

				// 重新创建管线以生效
				CreatePipeline(factory);

			}
			
			// 光源
			protected ResourceSet _lightingSet;
			protected ResourceLayout _lightingLayout;
			protected UniformBufferResource<LightingParams> _lightingUniform;
			public struct LightingParams
			{
				// slot 0
				public Vector3 LightDir;   public float _pad0;       // 16B

				// slot 1
				public Vector3 LightColor; public float _pad1;       // 16B

				// slot 2
				public Vector3 CameraPos;  public float Roughness;   // 16B

				// slot 3
				public float Metallic;     public float AO; 
				public float _pad2;        public float _pad3;       // 16B
			}
			
			public virtual void SetVertexData<T>(T[] vertices) where T : unmanaged
			{
				if (_graphicsDevice == null)
				{
					_pendingVertices = vertices;
					_pendingVerticesType = typeof(T);
					return;
				}

				CreateVertexBuffer(vertices);
			}

			private void CreateVertexBuffer<T>(T[] vertices) where T : unmanaged
			{
				_vertexBuffer?.Dispose();
				_vertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(
					new BufferDescription((uint)(vertices.Length * SizeOf<T>()), BufferUsage.VertexBuffer));
				_graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices);
			}

			public virtual void UpdateMatrices(CommandList commandList)
			{
				if (_matrixUniform == null || _graphicsDevice == null)
				{
					return;
				}

				var matrixUniform = new Matrix4x4Uniform
				{
					Model = ModelMatrix,
					View = ViewMatrix,
					Projection = ProjectionMatrix
				};

				_matrixUniform.Update(_graphicsDevice, ref matrixUniform);
			}

			public override void Draw(CommandList commandList)
			{
				if (_vertexBuffer == null || _pipeline == null) return;

				commandList.SetVertexBuffer(0, _vertexBuffer);
				commandList.SetPipeline(_pipeline);
				
				UpdateMatrices(commandList);
				commandList.SetGraphicsResourceSet(0, _resourceSet);   // set = 0
				commandList.SetGraphicsResourceSet(1, _lightingSet);   // set = 1

				UpdateMatrices(commandList);

				commandList.SetGraphicsResourceSet(0, _resourceSet);

				if (_indexBuffer != null)
				{
					commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
					commandList.DrawIndexed(
						indexCount: (uint)(_indexBuffer.SizeInBytes / sizeof(ushort)),
						instanceCount: 1,
						indexStart: 0,
						vertexOffset: 0,
						instanceStart: 0);
				}
				else
				{
					commandList.Draw((uint)(_vertexBuffer.SizeInBytes / VertexLayout.Elements.Sum(e => e.Format.GetSizeInBytes())));
				}
			}
			
			public void UpdateLighting(GraphicsDevice gd, in LightingParams p)
			{
				_lightingUniform?.Update(gd, ref Unsafe.AsRef(p));
			}

			public override void Dispose()
			{
				base.Dispose();
				_resourceSet?.Dispose();
				_resourceLayout?.Dispose();
				_matrixUniform?.Dispose();
			}
		}

		// 3D纹理渲染器
		public class TexturedRenderable3D : GeometryRenderable3D
		{
			private ResourceSet _textureResourceSet;
			private ResourceLayout _textureResourceLayout;
			private TextureView _textureView;

			public TexturedRenderable3D(string name, TextureView textureView, VertexLayoutDescription vertexLayout, GraphicsPipelineDescription pipelineDescription)
				: base(name, vertexLayout, pipelineDescription)
			{
				_textureView = textureView;
			}

			public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
			{
				base.Initialize(gd, factory);

				// 创建纹理资源布局
				_textureResourceLayout = factory.CreateResourceLayout(
					new ResourceLayoutDescription(
						new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
						new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)
					)
				);

				// 创建纹理资源集
				_textureResourceSet = factory.CreateResourceSet(
					new ResourceSetDescription(
						_textureResourceLayout,
						_textureView,
						_graphicsDevice.PointSampler
					)
				);

				// 更新管道描述以包含纹理资源布局
				var pipelineDesc = PipelineDescription;
				var existingLayouts = pipelineDesc.ResourceLayouts.ToList();
				existingLayouts.Add(_textureResourceLayout);
				pipelineDesc.ResourceLayouts = existingLayouts.ToArray();
				PipelineDescription = pipelineDesc;

				// 重新创建管道
				CreatePipeline(factory);
			}

			public override void Draw(CommandList commandList)
			{
				if (_vertexBuffer == null || _pipeline == null) return;

				commandList.SetPipeline(_pipeline);

				commandList.SetVertexBuffer(0, _vertexBuffer);
				commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);

				UpdateMatrices(commandList);

				commandList.SetGraphicsResourceSet(2, _textureResourceSet);

				commandList.DrawIndexed(36, 1, 0, 0, 0);
			}
			
			public override void Dispose()
			{
				base.Dispose();
				_textureResourceSet?.Dispose();
				_textureResourceLayout?.Dispose();
			}
		}
		
		public class MultiChannelTexturedRenderable : GeometryRenderable
		{
		    private readonly int _channelCount;
		    private readonly TextureView[] _channelViews;
		    private ResourceLayout _channelLayout;
		    private ResourceSet _channelSet;

		    /// <param name="channelCount">通道数（建议 2~4）。</param>
		    public MultiChannelTexturedRenderable(
		        string name,
		        int channelCount,
		        VertexLayoutDescription vertexLayout,
		        GraphicsPipelineDescription pipelineDescription)
		        : base(name, vertexLayout, pipelineDescription)
		    {
		        if (channelCount <= 0) throw new ArgumentOutOfRangeException(nameof(channelCount));
		        _channelCount = channelCount;
		        _channelViews = new TextureView[_channelCount];
		    }

		    /// <summary>设置指定通道的纹理视图（0-based）。</summary>
		    public void SetChannelView(int index, TextureView view)
		    {
		        if ((uint)index >= (uint)_channelCount) throw new ArgumentOutOfRangeException(nameof(index));
		        _channelViews[index] = view;
		        if (_graphicsDevice != null && _channelLayout != null)
		        {
		            _channelSet?.Dispose();
		            _channelSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
		                new ResourceSetDescription(_channelLayout, BuildResourceSetParams()));
		        }
		    }

		    /// <summary>批量设置通道纹理。</summary>
		    public void SetChannelViews(params TextureView[] views)
		    {
		        if (views == null || views.Length != _channelCount)
		            throw new ArgumentException($"需要提供 {_channelCount} 个通道纹理。");
		        for (int i = 0; i < _channelCount; i++) _channelViews[i] = views[i];
		        if (_graphicsDevice != null && _channelLayout != null)
		        {
		            _channelSet?.Dispose();
		            _channelSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
		                new ResourceSetDescription(_channelLayout, BuildResourceSetParams()));
		        }
		    }

		    protected override void CreatePipeline(ResourceFactory factory)
		    {
		        var elements = new List<ResourceLayoutElementDescription>(_channelCount * 2);
		        for (int i = 0; i < _channelCount; i++)
		        {
		            elements.Add(new ResourceLayoutElementDescription($"Channel{i}", ResourceKind.TextureReadOnly, ShaderStages.Fragment));
		            elements.Add(new ResourceLayoutElementDescription($"Channel{i}Sampler", ResourceKind.Sampler, ShaderStages.Fragment));
		        }
		        _channelLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(elements.ToArray()));

		        var pipeDesc = PipelineDescription;
		        var layouts = pipeDesc.ResourceLayouts?.ToList() ?? new List<ResourceLayout>();
		        layouts.Add(_channelLayout);
		        pipeDesc.ResourceLayouts = layouts.ToArray();
		        PipelineDescription = pipeDesc;

		        base.CreatePipeline(factory);

		        _channelSet?.Dispose();
		        _channelSet = factory.CreateResourceSet(new ResourceSetDescription(_channelLayout, BuildResourceSetParams()));
		    }

		    private BindableResource[] BuildResourceSetParams()
		    {
		        if (_graphicsDevice == null) return Array.Empty<BindableResource>();
		        var list = new List<BindableResource>(_channelCount * 2);
		        for (int i = 0; i < _channelCount; i++)
		        {
		            if (_channelViews[i] == null)
		                throw new InvalidOperationException($"Channel {i} 纹理尚未设置。");
		            list.Add(_channelViews[i]);
		            list.Add(_graphicsDevice.PointSampler);
		        }
		        return list.ToArray();
		    }

		    public override void Draw(CommandList cl)
		    {
		        if (_vertexBuffer == null || _pipeline == null) return;

		        cl.SetVertexBuffer(0, _vertexBuffer);
		        cl.SetPipeline(_pipeline);

		        cl.SetGraphicsResourceSet((uint)(PipelineDescription.ResourceLayouts.Length - 1), _channelSet);

		        if (_indexBuffer != null)
		        {
		            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
		            cl.DrawIndexed(
		                indexCount: (uint)(_indexBuffer.SizeInBytes / sizeof(ushort)),
		                instanceCount: 1, indexStart: 0, vertexOffset: 0, instanceStart: 0);
		        }
		        else
		        {
		            uint stride = (uint)VertexLayout.Elements.Sum(e => e.Format.GetSizeInBytes());
		            cl.Draw((uint)(_vertexBuffer.SizeInBytes / stride));
		        }
		    }

		    public override void Dispose()
		    {
		        base.Dispose();
		        _channelSet?.Dispose();
		        _channelLayout?.Dispose();
		    }
		}
		
		public class MultiChannelTexturedRenderable3D : GeometryRenderable3D
		{
		    private readonly int _channelCount;
		    private readonly TextureView[] _channelViews;
		    private ResourceLayout _channelLayout;
		    private ResourceSet _channelSet;

		    public MultiChannelTexturedRenderable3D(
		        string name, int channelCount,
		        VertexLayoutDescription vertexLayout,
		        GraphicsPipelineDescription pipelineDescription)
		        : base(name, vertexLayout, pipelineDescription)
		    {
		        if (channelCount <= 0) throw new ArgumentOutOfRangeException(nameof(channelCount));
		        _channelCount = channelCount;
		        _channelViews = new TextureView[_channelCount];
		    }

		    public void SetChannelView(int index, TextureView view)
		    {
		        if ((uint)index >= (uint)_channelCount) throw new ArgumentOutOfRangeException(nameof(index));
		        _channelViews[index] = view;
		        if (_graphicsDevice != null && _channelLayout != null)
		        {
		            _channelSet?.Dispose();
		            _channelSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
		                new ResourceSetDescription(_channelLayout, BuildParams()));
		        }
		    }

		    public void SetChannelViews(params TextureView[] views)
		    {
		        if (views == null || views.Length != _channelCount)
		            throw new ArgumentException($"需要提供 {_channelCount} 个通道纹理。");
		        for (int i = 0; i < _channelCount; i++) _channelViews[i] = views[i];
		        if (_graphicsDevice != null && _channelLayout != null)
		        {
		            _channelSet?.Dispose();
		            _channelSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
		                new ResourceSetDescription(_channelLayout, BuildParams()));
		        }
		    }

		    public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
		    {
		        base.Initialize(gd, factory);

		        var elems = new List<ResourceLayoutElementDescription>(_channelCount * 2);
		        for (int i = 0; i < _channelCount; i++)
		        {
		            elems.Add(new ResourceLayoutElementDescription($"Channel{i}", ResourceKind.TextureReadOnly, ShaderStages.Fragment));
		            elems.Add(new ResourceLayoutElementDescription($"Channel{i}Sampler", ResourceKind.Sampler, ShaderStages.Fragment));
		        }
		        _channelLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(elems.ToArray()));

		        var pd = PipelineDescription;
		        var layouts = pd.ResourceLayouts?.ToList() ?? new List<ResourceLayout>();
		        layouts.Add(_channelLayout);
		        pd.ResourceLayouts = layouts.ToArray();
		        PipelineDescription = pd;

		        CreatePipeline(factory);

		        _channelSet?.Dispose();
		        _channelSet = factory.CreateResourceSet(new ResourceSetDescription(_channelLayout, BuildParams()));
		    }

		    private BindableResource[] BuildParams()
		    {
		        if (_graphicsDevice == null) return Array.Empty<BindableResource>();
		        var list = new List<BindableResource>(_channelCount * 2);
		        for (int i = 0; i < _channelCount; i++)
		        {
		            if (_channelViews[i] == null)
		                throw new InvalidOperationException($"Channel {i} 纹理尚未设置。");
		            list.Add(_channelViews[i]);
		            list.Add(_graphicsDevice.PointSampler);
		        }
		        return list.ToArray();
		    }

		    public override void Draw(CommandList cl)
		    {
		        if (_vertexBuffer == null || _pipeline == null) return;

		        cl.SetPipeline(_pipeline);
		        cl.SetVertexBuffer(0, _vertexBuffer);
		        if (_indexBuffer != null) cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);

		        UpdateMatrices(cl);
		        cl.SetGraphicsResourceSet(0, _resourceSet);
		        cl.SetGraphicsResourceSet(1, _lightingSet);

		        cl.SetGraphicsResourceSet((uint)(PipelineDescription.ResourceLayouts.Length - 1), _channelSet);

		        if (_indexBuffer != null)
		            cl.DrawIndexed((uint)(_indexBuffer.SizeInBytes / sizeof(ushort)), 1, 0, 0, 0);
		        else
		            cl.Draw((uint)(_vertexBuffer.SizeInBytes / VertexLayout.Elements.Sum(e => e.Format.GetSizeInBytes())));
		    }

		    public override void Dispose()
		    {
		        base.Dispose();
		        _channelSet?.Dispose();
		        _channelLayout?.Dispose();
		    }
		}


		// 相机类
		public class Camera
		{
			public Vector3 Position { get; set; } = Vector3.Zero;
			public Vector3 Target { get; set; } = Vector3.UnitZ;
			public Vector3 Up { get; set; } = Vector3.UnitY;
			public float FieldOfView { get; set; } = MathF.PI / 3f; // 60度
			public float AspectRatio { get; set; } = 16f / 9f;
			public float NearPlane { get; set; } = 0.1f;
			public float FarPlane { get; set; } = 1000f;

			// 添加移动速度属性
			public float MoveSpeed { get; set; } = 5.0f;
			public float RotationSpeed { get; set; } = 2.0f;

			private Vector3 _velocity;
			private float _yaw;
			private float _pitch;

			// 计算前向向量
			public Vector3 Forward => Vector3.Normalize(Target - Position);

			// 计算右向向量
			public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Up));

			// 计算上向向量
			public Vector3 UpVector => Vector3.Normalize(Up);

			public Matrix4x4 GetViewMatrix()
			{
				return Matrix4x4.CreateLookAt(Position, Target, Up);
			}

			public Matrix4x4 GetProjectionMatrix()
			{
				return Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
			}

			// 移动相机方法
			public void Move(Vector3 direction, float deltaTime)
			{
				Position += direction * MoveSpeed * deltaTime;
				Target += direction * MoveSpeed * deltaTime;
			}

			// 旋转相机方法
			public void Rotate(float yaw, float pitch, float deltaTime)
			{
				// 计算旋转矩阵
				Matrix4x4 yawRotation = Matrix4x4.CreateFromAxisAngle(UpVector, yaw * RotationSpeed * deltaTime);
				Matrix4x4 pitchRotation = Matrix4x4.CreateFromAxisAngle(Right, pitch * RotationSpeed * deltaTime);

				// 应用旋转到前向向量
				Vector3 newForward = Vector3.Transform(Forward, pitchRotation * yawRotation);

				// 更新目标点
				Target = Position + newForward;
			}

			/// <summary>
			/// 更新相机状态
			/// </summary>
			public void Update(float deltaTime)
			{
				// 应用速度移动相机
				if (_velocity != Vector3.Zero)
				{
					Position += _velocity * deltaTime;
					Target += _velocity * deltaTime;
					_velocity = Vector3.Zero; // 重置速度
				}
			}

			/// <summary>
			/// 设置相机移动速度
			/// </summary>
			public void SetVelocity(Vector3 velocity)
			{
				_velocity = velocity;
			}

			/// <summary>
			/// 获取相机的偏航角（Yaw）
			/// </summary>
			public float GetYaw()
			{
				return _yaw;
			}

			/// <summary>
			/// 获取相机的俯仰角（Pitch）
			/// </summary>
			public float GetPitch()
			{
				return _pitch;
			}

			/// <summary>
			/// 设置相机的偏航角和俯仰角
			/// </summary>
			public void SetRotation(float yaw, float pitch)
			{
				_yaw = yaw;
				_pitch = Math.Clamp(pitch, -MathF.PI / 2.0f + 0.1f, MathF.PI / 2.0f - 0.1f);

				// 根据偏航角和俯仰角计算前向向量
				Vector3 front;
				front.X = MathF.Cos(_yaw) * MathF.Cos(_pitch);
				front.Y = MathF.Sin(_pitch);
				front.Z = MathF.Sin(_yaw) * MathF.Cos(_pitch);

				Target = Position + Vector3.Normalize(front);
			}
		}
	}

	// 顶点结构体
	public struct VertexPositionColor
	{
		public const uint SizeInBytes = 24;
		public Vector2 Position;
		public RgbaFloat Color;

		public VertexPositionColor(Vector2 position, RgbaFloat color)
		{
			Position = position;
			Color = color;
		}

		public static VertexLayoutDescription Layout => new VertexLayoutDescription(
			new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
			new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
	}

	// 带纹理的顶点结构体
	public struct VertexPositionTexture
	{
		public const uint SizeInBytes = 20;
		public Vector2 Position;
		public Vector2 TexCoord;

		public VertexPositionTexture(Vector2 position, Vector2 texCoord)
		{
			Position = position;
			TexCoord = texCoord;
		}

		public static VertexLayoutDescription Layout => new VertexLayoutDescription(
			new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
			new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
	}

	// 纹理资源
	public class TextureResource : RenderResource
	{
		private Texture _texture;
		private TextureView _textureView;
		private string _filePath;

		public string FilePath => _filePath;

		public TextureResource(string name, string filePath)
		{
			Name = name;
			_filePath = filePath;
		}

		public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
		{
			try
			{
				var image = new ImageSharpTexture(Path.Combine(Global.ResourcesPath, "textures", _filePath));

				_texture = image.CreateDeviceTexture(gd, factory);

				if ((_texture.Usage & TextureUsage.Sampled) == 0)
				{
					var textureDesc = new TextureDescription(
						_texture.Width,
						_texture.Height,
						_texture.Depth,
						_texture.MipLevels,
						_texture.ArrayLayers,
						_texture.Format,
						_texture.Usage | TextureUsage.Sampled,
						_texture.Type
					);

					var newTexture = factory.CreateTexture(textureDesc);

					using (var commandList = factory.CreateCommandList())
					{
						commandList.Begin();
						commandList.CopyTexture(_texture, newTexture);
						commandList.End();
						gd.SubmitCommands(commandList);
					}

					_texture.Dispose();
					_texture = newTexture;
				}

				_textureView = factory.CreateTextureView(_texture);
			}
			catch (Exception ex)
			{
				throw new Exception($"Failed to initialize texture resource '{Name}' from file '{_filePath}': {ex.Message}");
			}
		}

		public TextureView GetTextureView() => _textureView;

		public override void Dispose()
		{
			_textureView?.Dispose();
			_texture?.Dispose();
		}
	}

	// 着色器资源
	public class ShaderResource : RenderResource
	{
		private Shader[] _shaders;
		private string _vertexShaderPath;
		private string _fragmentShaderPath;
		private byte[] _vertexShaderBytes;
		private byte[] _fragmentShaderBytes;

		private RendererConfig _config;

		public string VertexShaderPath => _vertexShaderPath;
		public string FragmentShaderPath => _fragmentShaderPath;

		public ShaderResource(string name, string vertexShaderPath, string fragmentShaderPath, RendererConfig config = null)
		{
			Name = name;
			_vertexShaderPath = vertexShaderPath;
			_fragmentShaderPath = fragmentShaderPath;
			_config = config;
		}

		public ShaderResource(string name, byte[] vertexShaderBytes, byte[] fragmentShaderBytes, RendererConfig config = null)
		{
			Name = name;
			_vertexShaderBytes = vertexShaderBytes;
			_fragmentShaderBytes = fragmentShaderBytes;
			_config = config;
		}

		public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
		{
			if (_vertexShaderBytes != null && _fragmentShaderBytes != null)
			{
				_shaders = factory.CreateFromSpirv(
					new ShaderDescription(ShaderStages.Vertex, _vertexShaderBytes, "main"),
					new ShaderDescription(ShaderStages.Fragment, _fragmentShaderBytes, "main")
				);
			}
			else if (!string.IsNullOrEmpty(_vertexShaderPath) && !string.IsNullOrEmpty(_fragmentShaderPath))
			{
				if (File.Exists(_vertexShaderPath) && File.Exists(_fragmentShaderPath))
				{
					_shaders = factory.CreateFromSpirv(
						new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes(_vertexShaderPath), "main"),
						new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes(_fragmentShaderPath), "main")
					);
				}
				else
				{
					throw new FileNotFoundException($"Shader files not found: {_vertexShaderPath} or {_fragmentShaderPath}");
				}
			}
			else
			{
				throw new InvalidOperationException("No shader source provided. Set either shader paths or shader bytes.");
			}
		}
		public Shader[] GetShaders() => _shaders;

		public override void Dispose()
		{
			if (_shaders != null)
			{
				foreach (var shader in _shaders)
				{
					shader?.Dispose();
				}
			}
		}
	}

	public class TriangleRenderer : GeometryRenderable
	{
		public TriangleRenderer(string name, string vertexShaderPath = null, string fragmentShaderPath = null)
			: base(name, VertexPositionColor.Layout, CreatePipelineDescription())
		{
			if (!string.IsNullOrEmpty(vertexShaderPath) && !string.IsNullOrEmpty(fragmentShaderPath))
			{
				VertexShaderPath = vertexShaderPath;
				FragmentShaderPath = fragmentShaderPath;
			}
		}

		public TriangleRenderer(string name, byte[] vertexShaderBytes, byte[] fragmentShaderBytes)
			: base(name, VertexPositionColor.Layout, CreatePipelineDescription())
		{
			VertexShaderBytes = vertexShaderBytes;
			FragmentShaderBytes = fragmentShaderBytes;
		}

		private static GraphicsPipelineDescription CreatePipelineDescription()
		{
			return new GraphicsPipelineDescription
			{
				BlendState = BlendStateDescription.SingleOverrideBlend,
				DepthStencilState = new DepthStencilStateDescription(
					depthTestEnabled: true,
					depthWriteEnabled: true,
					comparisonKind: ComparisonKind.LessEqual),
				RasterizerState = new RasterizerStateDescription(
					cullMode: FaceCullMode.Back,
					fillMode: PolygonFillMode.Solid,
					frontFace: FrontFace.Clockwise,
					depthClipEnabled: true,
					scissorTestEnabled: false),
				PrimitiveTopology = PrimitiveTopology.TriangleList,
				ResourceLayouts = Array.Empty<ResourceLayout>()
			};
		}

		public void SetTriangleVertices(VertexPositionColor[] vertices)
		{
			SetVertexData(vertices);
		}
	}

	public class TexturedRectangleRenderer : TexturedRenderable
	{
		public TexturedRectangleRenderer(string name, TextureView textureView, string vertexShaderPath = null, string fragmentShaderPath = null)
			: base(name, textureView, VertexPositionTexture.Layout, CreatePipelineDescription())
		{
			if (!string.IsNullOrEmpty(vertexShaderPath) && !string.IsNullOrEmpty(fragmentShaderPath))
			{
				VertexShaderPath = vertexShaderPath;
				FragmentShaderPath = fragmentShaderPath;
			}
		}

		public TexturedRectangleRenderer(string name, TextureView textureView, byte[] vertexShaderBytes, byte[] fragmentShaderBytes)
			: base(name, textureView, VertexPositionTexture.Layout, CreatePipelineDescription())
		{
			VertexShaderBytes = vertexShaderBytes;
			FragmentShaderBytes = fragmentShaderBytes;
		}

		private static GraphicsPipelineDescription CreatePipelineDescription()
		{
			return new GraphicsPipelineDescription
			{
				BlendState = BlendStateDescription.SingleOverrideBlend,
				DepthStencilState = new DepthStencilStateDescription(
					depthTestEnabled: true,
					depthWriteEnabled: true,
					comparisonKind: ComparisonKind.LessEqual),
				RasterizerState = new RasterizerStateDescription(
					cullMode: FaceCullMode.Back,
					fillMode: PolygonFillMode.Solid,
					frontFace: FrontFace.Clockwise,
					depthClipEnabled: true,
					scissorTestEnabled: false),
				PrimitiveTopology = PrimitiveTopology.TriangleList
			};
		}

		public void SetRectangleVertices(VertexPositionTexture[] vertices, ushort[] indices)
		{
			SetVertexData(vertices);
			SetIndexData(indices);
		}
	}
}