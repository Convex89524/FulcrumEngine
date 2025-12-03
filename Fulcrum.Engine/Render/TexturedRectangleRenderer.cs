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

using Veldrid;

namespace Fulcrum.Engine.Render
{
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