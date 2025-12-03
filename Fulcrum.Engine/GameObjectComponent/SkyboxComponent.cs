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
using System.Numerics;
using System.Runtime.InteropServices;
using CMLS.CLogger;
using Veldrid;
using Fulcrum.Engine;
using Fulcrum.Engine.GameObjectComponent;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Render.Shaders;
using Fulcrum.Engine.Scene;
using IRenderable = Fulcrum.Engine.Render.IRenderable;

namespace Fulcrum.Engine.GameObjectComponent
{
	[StructLayout(LayoutKind.Sequential)]
	public struct SkyboxUBO
	{
		public Matrix4x4 Projection;
		public Matrix4x4 Model;
		public Matrix4x4 Normal;
		public Matrix4x4 View;
	}

	public struct SkyboxVertex
	{
		public Vector3 Position;

		public SkyboxVertex(Vector3 pos)
		{
			Position = pos;
		}

		public static readonly VertexLayoutDescription Layout =
			new VertexLayoutDescription(
				new VertexElementDescription(
					"inPos",
					VertexElementSemantic.Position,
					VertexElementFormat.Float3));
	}

	public sealed class SkyboxRenderable : IRenderable
	{
		private static readonly Clogger LOGGER = LogManager.GetLogger("SkyboxRenderable");

		private readonly RendererBase _renderer;
		private readonly ShaderFramework _shaderFramework;
		private readonly string _programName;
		private readonly string? _programVariant;

		private GraphicsDevice _gd;
		private DeviceBuffer _vertexBuffer;
		private DeviceBuffer _indexBuffer;
		private Pipeline _pipeline;

		private ResourceLayout _resourceLayout;
		private ResourceSet _resourceSet;
		private TextureView _cubeTextureView;

		private VertexLayoutDescription _vertexLayout;

		private UniformBufferResource<SkyboxUBO> _ubo;

		private static readonly ShaderProgramKey s_programKey =
			new ShaderProgramKey("Skybox", "Default");

		private bool _programRegistered = false;

		public string Name { get; }
		public float Size { get; set; } = 1.0f;

		public SkyboxRenderable(
			string name,
			RendererBase renderer,
			ShaderFramework shaderFramework,
			TextureView initialCubeTexture = null,
			string programName = "Skybox",
			string? programVariant = null)
		{
			Name = name ?? "Skybox";
			_renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
			_shaderFramework = shaderFramework ?? throw new ArgumentNullException(nameof(shaderFramework));
			_programName = programName;
			_programVariant = programVariant;

			_vertexLayout = SkyboxVertex.Layout;
			_cubeTextureView = initialCubeTexture;
		}

		public void Initialize(GraphicsDevice gd, ResourceFactory factory)
		{
			_gd = gd ?? throw new ArgumentNullException(nameof(gd));
			if (factory == null) throw new ArgumentNullException(nameof(factory));

			EnsureProgramRegistered();

			_ubo = new UniformBufferResource<SkyboxUBO>($"{Name}_UBO");
			_ubo.Initialize(gd, factory);

			_resourceLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription(
						"UBO",
						ResourceKind.UniformBuffer,
						ShaderStages.Vertex),
					new ResourceLayoutElementDescription(
						"samplerCubeMap",
						ResourceKind.TextureReadOnly,
						ShaderStages.Fragment)
				)
			);

			CreateCubeGeometry(factory);

			CreatePipeline(factory);

			if (_cubeTextureView != null)
			{
				_resourceSet = factory.CreateResourceSet(
					new ResourceSetDescription(
						_resourceLayout,
						_ubo.GetBuffer(),
						_cubeTextureView
					)
				);
			}
			else
			{
				LOGGER.Warn("SkyboxRenderable 初始化时没有绑定 CubeMap 纹理，画面将是空的，需调用 SetCubeTexture(...) 之后才能显示。");
			}
		}

		private void EnsureProgramRegistered()
		{
			if (_programRegistered) return;

			_shaderFramework.RegisterProgramFromFiles(
				_programName,
				_programVariant,
				"vulkanscene/skybox.vert.spv",
				"vulkanscene/skybox.frag.spv");

			_programRegistered = true;
		}

		private void CreateCubeGeometry(ResourceFactory factory)
		{
			float s = Size;

			var vertices = new SkyboxVertex[]
			{
				new SkyboxVertex(new Vector3(-s, -s,  s)),
				new SkyboxVertex(new Vector3( s, -s,  s)),
				new SkyboxVertex(new Vector3( s,  s,  s)),
				new SkyboxVertex(new Vector3(-s,  s,  s)),

				new SkyboxVertex(new Vector3(-s, -s, -s)),
				new SkyboxVertex(new Vector3( s, -s, -s)),
				new SkyboxVertex(new Vector3( s,  s, -s)),
				new SkyboxVertex(new Vector3(-s,  s, -s)),
			};

			ushort[] indices =
			{
				// 前
				0, 1, 2, 2, 3, 0,
				// 右
				1, 5, 6, 6, 2, 1,
				// 后
				5, 4, 7, 7, 6, 5,
				// 左
				4, 0, 3, 3, 7, 4,
				// 上
				3, 2, 6, 6, 7, 3,
				// 下
				4, 5, 1, 1, 0, 4
			};

			_vertexBuffer = _gd.ResourceFactory.CreateBuffer(
				new BufferDescription(
					(uint)(vertices.Length * Marshal.SizeOf<SkyboxVertex>()),
					BufferUsage.VertexBuffer));

			_gd.UpdateBuffer(_vertexBuffer, 0, vertices);

			_indexBuffer = _gd.ResourceFactory.CreateBuffer(
				new BufferDescription(
					(uint)(indices.Length * sizeof(ushort)),
					BufferUsage.IndexBuffer));

			_gd.UpdateBuffer(_indexBuffer, 0, indices);
		}

		private void CreatePipeline(ResourceFactory factory)
		{
			var passDesc = ShaderPassDescriptor.CreateBasic(
				new ShaderProgramKey(_programName, _programVariant),
				_vertexLayout,
				resourceLayouts: new[] { _resourceLayout },

				depthTest: false,
				depthWrite: false,

				topology: PrimitiveTopology.TriangleList,
				cull: FaceCullMode.None,
				frontFace: FrontFace.Clockwise,
				blend: BlendStateDescription.SingleOverrideBlend);

			_pipeline = _shaderFramework.GetOrCreatePipeline(passDesc);
		}

		public void SetCubeTexture(TextureView textureView)
		{
			_cubeTextureView = textureView ?? throw new ArgumentNullException(nameof(textureView));
			if (_gd == null || _resourceLayout == null || _ubo == null)
			{
				return;
			}

			_resourceSet?.Dispose();
			_resourceSet = _gd.ResourceFactory.CreateResourceSet(
				new ResourceSetDescription(
					_resourceLayout,
					_ubo.GetBuffer(),
					_cubeTextureView
				)
			);
		}

		public void Draw(CommandList commandList)
		{
			if (_pipeline == null || _vertexBuffer == null || _indexBuffer == null) return;
			if (_renderer?.Camera == null) return;
			if (_resourceSet == null) return;

			var cam = _renderer.Camera;
			var proj = cam.GetProjectionMatrix();
			var view = cam.GetViewMatrix();
			var model = Matrix4x4.Identity;
			var normal = Matrix4x4.Identity;

			var uboData = new SkyboxUBO
			{
				Projection = proj,
				Model = model,
				Normal = normal,
				View = view
			};
			_ubo.Update(_gd, ref uboData);

			commandList.SetPipeline(_pipeline);
			commandList.SetVertexBuffer(0, _vertexBuffer);
			commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
			commandList.SetGraphicsResourceSet(0, _resourceSet);

			commandList.DrawIndexed(
				indexCount: 36,
				instanceCount: 1,
				indexStart: 0,
				vertexOffset: 0,
				instanceStart: 0);
		}

		public void Dispose()
		{
			try
			{
				_vertexBuffer?.Dispose();
				_indexBuffer?.Dispose();
				_pipeline?.Dispose();
				_resourceSet?.Dispose();
				_resourceLayout?.Dispose();
				_ubo?.Dispose();
			}
			catch (Exception e)
			{
				LOGGER.Warn($"Dispose SkyboxRenderable failed: {e}");
			}
		}
	}
}

namespace Fulcrum.Engine.GameObjectComponent
{
	public sealed class SkyboxComponent : Component
	{
		private static readonly Clogger LOGGER = LogManager.GetLogger("SkyboxComponent");

		private SkyboxRenderable _renderable;
		private RendererBase _renderer;

		public string SkyboxName { get; set; } = "Skybox";

		public float Size { get; set; } = 1.0f;

		private TextureView _initialCubeTexture;

		protected override void Awake()
		{
			base.Awake();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			TryCreateAndRegisterRenderable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			UnregisterRenderable();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			UnregisterRenderable();
		}

		private void TryCreateAndRegisterRenderable()
		{
			if (_renderable != null) return;

			if (FulcrumEngine.RenderApp == null)
			{
				LOGGER.Warn("SkyboxComponent: FulcrumEngine.RenderApp 为空，无法创建 SkyboxRenderable。请在引擎初始化 RenderApp 之后再创建 Skybox。");
				return;
			}

			_renderer = FulcrumEngine.RenderApp.Renderer;
			if (_renderer == null)
			{
				LOGGER.Warn("SkyboxComponent: RenderApp.Renderer 为空，无法创建 SkyboxRenderable。");
				return;
			}

			if (_renderer.ShaderFramework == null)
			{
				LOGGER.Warn("SkyboxComponent: Renderer.ShaderFramework 为空，请确认着色器框架已初始化。");
				return;
			}

			_renderable = new SkyboxRenderable(
				name: SkyboxName,
				renderer: _renderer,
				shaderFramework: _renderer.ShaderFramework,
				initialCubeTexture: _initialCubeTexture);

			_renderable.Size = Size;

			_renderer.AddRenderable(_renderable);
			LOGGER.Info($"SkyboxComponent: SkyboxRenderable '{_renderable.Name}' registered to renderer.");
		}

		private void UnregisterRenderable()
		{
			if (_renderer != null && _renderable != null)
			{
				try
				{
					_renderer.RemoveRenderable(_renderable.Name);
				}
				catch (Exception e)
				{
					LOGGER.Warn($"SkyboxComponent: RemoveRenderable failed: {e}");
				}

				_renderable.Dispose();
				_renderable = null;
				LOGGER.Info("SkyboxComponent: SkyboxRenderable disposed and unregistered.");
			}
		}

		public void BindCubeMapTexture(TextureView cubeTextureView)
		{
			if (cubeTextureView == null) throw new ArgumentNullException(nameof(cubeTextureView));

			_initialCubeTexture = cubeTextureView;
			if (_renderable != null)
			{
				TryCreateAndRegisterRenderable();
			}
			_renderable.SetCubeTexture(cubeTextureView);
		}
	}
}
