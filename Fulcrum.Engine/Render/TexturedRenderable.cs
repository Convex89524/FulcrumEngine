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
}