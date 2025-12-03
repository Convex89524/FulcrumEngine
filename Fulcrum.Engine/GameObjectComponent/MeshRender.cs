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

using System.Numerics;
using CMLS.CLogger;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Scene;
using Veldrid;

namespace Fulcrum.Engine.GameObjectComponent
{
    public class MeshRenderer : Component
    {
        public string RenderableName
        {
            get => _renderableName ?? $"{Owner?.Name ?? "GameObject"}_Mesh";
            set => _renderableName = value;
        }

        private string _renderableName;

        public VertexPositionNormalTexture[] Vertices { get; set; }

        public ushort[] Indices { get; set; }

        public string VertexShaderPath { get; set; } =
            Path.Combine("shaders", "vulkanscene", "mesh.vert");

        public string FragmentShaderPath { get; set; } =
            Path.Combine("shaders", "vulkanscene", "mesh.frag");
        
        public string? AlbedoTexturePath { get; set; }

        public bool UseAlbedoTexture { get; set; } = true;
        
        public bool AutoModelFromTransform { get; set; } = true;

        public Matrix4x4 ManualModelMatrix
        {
            get => _manualModelMatrix;
            set
            {
                _manualModelMatrix = value;
                if (_renderable != null)
                    _renderable.ModelMatrix = value;
            }
        }

        private Matrix4x4 _manualModelMatrix = Matrix4x4.Identity;

        private GeometryRenderable3D _renderable;
        private RendererBase _renderer;
        private bool _registeredToRenderer;

        #region 生命周期

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TryRegisterToRenderer();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            TryUnregisterFromRenderer();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            TryUnregisterFromRenderer();
        }

        protected override void Update(double dt)
        {
            base.Update(dt);

            if (_renderable == null || !_registeredToRenderer)
                return;

            if (Owner == null)
                return;

            if (AutoModelFromTransform)
            {
                var model = Owner.Transform.LocalToWorldMatrix;
                _renderable.ModelMatrix = model;
                _manualModelMatrix = model;
            }
            else
            {
                _renderable.ModelMatrix = _manualModelMatrix;
            }
        }

        #endregion

        #region 渲染器注册 / 注销

        private void TryRegisterToRenderer()
        {
            if (_registeredToRenderer)
                return;

            if (FulcrumEngine.RenderApp == null || FulcrumEngine.RenderApp.Renderer == null)
            {
                return;
            }

            _renderer = FulcrumEngine.RenderApp.Renderer;

            if (_renderable == null)
            {
                _renderable = CreateGeometryRenderable(_renderer);
            }

            try
            {
                _renderer.AddRenderable(_renderable);
                _registeredToRenderer = true;

                ApplyMeshToRenderable();

                if (Owner != null && AutoModelFromTransform)
                {
                    var model = Owner.Transform.LocalToWorldMatrix;
                    _manualModelMatrix = model;
                    _renderable.ModelMatrix = model;
                }
                else
                {
                    _renderable.ModelMatrix = _manualModelMatrix;
                }
            }
            catch (Exception e)
            {
                LOGGER.Warn($"MeshRenderer TryRegisterToRenderer failed: {e}");
            }
        }

        private void TryUnregisterFromRenderer()
        {
            if (!_registeredToRenderer || _renderer == null || _renderable == null)
                return;

            try
            {
                _renderer.RemoveRenderable(_renderable.Name);
                _registeredToRenderer = false;
            }
            catch (Exception e)
            {
                LOGGER.Warn($"MeshRenderer TryUnregisterFromRenderer failed: {e}");
            }
        }

        #endregion

        #region Mesh / Renderable 构建

        public void SetMesh(VertexPositionNormalTexture[] vertices, ushort[] indices)
        {
            Vertices = vertices;
            Indices  = indices;
            ApplyMeshToRenderable();
        }

        public void ApplyMeshToRenderable()
        {
            if (_renderable == null)
                return;

            if (Vertices != null && Vertices.Length > 0)
            {
                try
                {
                    _renderable.SetVertexData(Vertices);
                }
                catch (Exception e)
                {
                    LOGGER.Warn($"MeshRenderer ApplyMeshToRenderable vertex failed: {e}");
                }
            }

            if (Indices != null && Indices.Length > 0)
            {
                try
                {
                    _renderable.SetIndexData(Indices);
                }
                catch (Exception e)
                {
                    LOGGER.Warn($"MeshRenderer ApplyMeshToRenderable index failed: {e}");
                }
            }
        }

        private GeometryRenderable3D CreateGeometryRenderable(RendererBase renderer)
        {
            var layout = VertexPositionNormalTexture.Layout;

            var pipelineDesc = CreateDefaultMeshPipelineDescription(renderer._config.DepthFormat);

            var renderable = new MeshRenderable3D_internal(
                RenderableName,
                layout,
                pipelineDesc)
            {
                VertexShaderPath   = VertexShaderPath,
                FragmentShaderPath = FragmentShaderPath
            };

            return renderable;
        }

        private static GraphicsPipelineDescription CreateDefaultMeshPipelineDescription(PixelFormat? depthFormat)
        {
            var blend = BlendStateDescription.SingleOverrideBlend;

            var depth = new DepthStencilStateDescription(
                depthTestEnabled: depthFormat.HasValue,
                depthWriteEnabled: depthFormat.HasValue,
                comparisonKind: ComparisonKind.LessEqual);

            var rasterizer = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false);

            return new GraphicsPipelineDescription(
                blend,
                depth,
                rasterizer,
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    vertexLayouts: new[] { VertexPositionNormalTexture.Layout },
                    shaders: Array.Empty<Shader>()
                ),
                Array.Empty<ResourceLayout>(),
                new OutputDescription(
                    depthFormat.HasValue
                        ? new OutputAttachmentDescription(depthFormat.Value)
                        : default,
                    new OutputAttachmentDescription(PixelFormat.B8_G8_R8_A8_UNorm)
                )
            );
        }

        #endregion

        private static Matrix4x4? TryBuildModelFromTransform(GameObject go)
        {
            if (go == null || go.Transform == null)
                return null;

            return go.Transform.LocalToWorldMatrix;
        }

        private sealed class MeshRenderable3D_internal : GeometryRenderable3D
        {
            public MeshRenderable3D_internal(
                string name,
                VertexLayoutDescription vertexLayout,
                GraphicsPipelineDescription pipelineDescription)
                : base(name, vertexLayout, pipelineDescription)
            {
            }
        }
    }
}
