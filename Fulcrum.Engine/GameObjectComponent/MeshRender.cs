﻿// Copyright (C) 2025-2029 Convex89524
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

using System;
using System.Numerics;
using Fulcrum.Common;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Render.Utils;
using Fulcrum.Engine.Scene;
using Veldrid;
using TextureResource = Fulcrum.Engine.Render.TextureResource;

namespace Fulcrum.Engine.GameObjectComponent
{
    public class MeshRenderer : Component
    {
        public static string DefaultVertexShaderPath { get; set; } =
            "shaders/vulkanscene/mesh.vert";
        public static string DefaultFragmentShaderPath { get; set; } =
            "shaders/vulkanscene/mesh.frag";

        private VertexPositionNormalTexture[] _vertices;
        private ushort[] _indices;

        public MultiChannelTexturedRenderable3D Renderable { get; private set; }

        private TextureView[] _textureChannels = new TextureView[4];

        public float Roughness { get; set; } = 0.5f;
        public float Metallic  { get; set; } = 0.0f;
        public float Ao        { get; set; } = 1.0f;

        public string RenderableName =>
            $"{Owner?.Name ?? "GameObject"}_MeshRenderer";

        #region 生命周期

        protected override void OnEnable()
        {
            // Simple render mode: no light system required.
        }

        protected override void OnDisable()
        {
            // Simple render mode: no light system required.
        }

        protected override void OnDestroy()
        {
            Renderable?.Dispose();
            Renderable = null;
        }

        protected override void Update(double deltaTime)
        {
            if (Renderable != null && Owner != null && Owner.Transform != null)
            {
                Renderable.ModelMatrix = Owner.Transform.LocalToWorldMatrix;

                var renderer = FulcrumEngine.RenderApp?.Renderer;
                var gd = renderer?._graphicsDevice;
                if (gd != null)
                {
                    var cam = renderer.Camera;
                    var camPos = cam != null ? cam.Position : new Vector3(0, 0, 5);

                    var lp = new GeometryRenderable3D.LightingParams
                    {
                        LightDir = Vector3.Normalize(new Vector3(-0.35f, -1.0f, -0.25f)),
                        LightColor = new Vector3(1.0f, 1.0f, 1.0f),
                        CameraPos = camPos,
                        Roughness = Roughness,
                        Metallic = Metallic,
                        AO = Ao
                    };

                    Renderable.UpdateLighting(gd, lp);
                }
            }
        }

        #endregion

        #region 网格 & 贴图 API

        public void SetMesh(
            VertexPositionNormalTexture[] vertices,
            ushort[] indices)
        {
            _vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            _indices  = indices  ?? throw new ArgumentNullException(nameof(indices));

            if (Renderable != null)
            {
                Renderable.SetMeshData(_vertices, _indices);
            }
        }
        
        #endregion

        #region Renderable 创建

        public MultiChannelTexturedRenderable3D CreateRenderable()
        {
            if (_vertices == null || _indices == null)
            {
                throw new InvalidOperationException(
                    "MeshRenderer.CreateRenderable: 请先调用 SetMesh() 设置顶点和索引。");
            }

            if (Renderable != null)
            {
                return Renderable;
            }

            var pipelineDesc = CreateDefaultPipelineDescription();

            var renderable = new MultiChannelTexturedRenderable3D(
                RenderableName,
                4,
                VertexPositionNormalTexture.Layout,
                pipelineDesc);

            renderable.VertexShaderPath   = DefaultVertexShaderPath;
            renderable.FragmentShaderPath = DefaultFragmentShaderPath;

            renderable.SetChannelViews(_textureChannels);

            renderable.SetMeshData(_vertices, _indices);

            Renderable = renderable;
            return Renderable;
        }

        private static GraphicsPipelineDescription CreateDefaultPipelineDescription()
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
                ResourceLayouts = Array.Empty<ResourceLayout>(),
            };
        }

        #endregion
    }
}
