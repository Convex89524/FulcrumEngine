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

using System.Linq;
using Veldrid;

namespace Fulcrum.Engine.Render
{
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

            // 重新创建管线
            CreatePipeline(factory);
        }

        public override void Draw(CommandList commandList)
        {
            if (_vertexBuffer == null || _pipeline == null) return;

            commandList.SetVertexBuffer(0, _vertexBuffer);
            commandList.SetPipeline(_pipeline);

            UpdateMatrices(commandList);
            commandList.SetGraphicsResourceSet(0, _resourceSet);
            commandList.SetGraphicsResourceSet(1, _lightingSet);

            if (_indexBuffer != null)
            {
                commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
                commandList.DrawIndexed(
                    indexCount: (uint)(_indexBuffer.SizeInBytes / sizeof(ushort)),
                    instanceCount: 1, indexStart: 0, vertexOffset: 0, instanceStart: 0);
            }
            else
            {
                commandList.Draw((uint)(_vertexBuffer.SizeInBytes /
                                        VertexLayout.Elements.Sum(e => e.Format.GetSizeInBytes())));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _textureResourceSet?.Dispose();
            _textureResourceLayout?.Dispose();
        }
    }
}