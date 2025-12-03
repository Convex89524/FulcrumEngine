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

using System.Numerics;
using CMLS.CLogger;
using Fulcrum.Engine.Render;
using Veldrid;

namespace Fulcrum.Engine.Render.Renderables
{
    /// <summary>
    /// 全屏黑洞渲染器：画一个覆盖屏幕的矩形，片元里做黑洞 + 引力透镜 + 星野噪声。
    /// </summary>
    public sealed class BlackHoleRenderable : GeometryRenderable
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("BlackHole");

        // 黑洞参数 UBO
        public struct BlackHoleParams
        {
            // t
            public float Time;

            // 黑洞事件视界的球半径（世界空间）
            public float HorizonRadius;

            // 引力透镜强度
            public float LensStrength;

            // 星星亮度
            public float StarBrightness;

            // 噪声尺度（越大星星越密）
            public float NoiseScale;

            // 噪声强度（云气）
            public float NoiseIntensity;

            // 曝光
            public float Exposure;

            // 对齐用
            public float Pad0;

            // xyz = Camera Position, w = FOV（弧度）
            public Vector4 CameraPos_Fov;

            // xyz = Camera Forward
            public Vector4 CameraForward_Pad;

            // xyz = Camera Right
            public Vector4 CameraRight_Pad;

            // xyz = Camera Up, w = AspectRatio
            public Vector4 CameraUp_Aspect;
        }

        private UniformBufferResource<BlackHoleParams> _paramsUniform;
        private ResourceLayout _resourceLayout;
        private ResourceSet _resourceSet;

        public BlackHoleRenderable(
            string name,
            VertexLayoutDescription vertexLayout,
            GraphicsPipelineDescription pipelineDescription)
            : base(name, vertexLayout, pipelineDescription)
        {
        }

        public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
        {
            _graphicsDevice = gd;

            // 1. 创建 UBO
            _paramsUniform = new UniformBufferResource<BlackHoleParams>($"{Name}_Params");
            _paramsUniform.Initialize(gd, factory);

            // 2. 创建资源布局（只有一个 UBO，没有纹理）
            _resourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(
                        "BlackHoleParams",
                        ResourceKind.UniformBuffer,
                        ShaderStages.Fragment)
                )
            );

            // 3. 更新管线描述
            var pipelineDesc = PipelineDescription;
            pipelineDesc.ResourceLayouts = new[] { _resourceLayout };
            PipelineDescription = pipelineDesc;

            // 4. 创建资源集
            _resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _resourceLayout,
                _paramsUniform.GetBuffer()
            ));

            // 5. 构建全屏矩形
            var vertices = new VertexPositionTexture[]
            {
                new VertexPositionTexture(new Vector2(-1f, -1f), new Vector2(0f, 0f)),
                new VertexPositionTexture(new Vector2( 1f, -1f), new Vector2(1f, 0f)),
                new VertexPositionTexture(new Vector2( 1f,  1f), new Vector2(1f, 1f)),
                new VertexPositionTexture(new Vector2(-1f,  1f), new Vector2(0f, 1f)),
            };
            var indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
            SetVertexData(vertices);
            SetIndexData(indices);

            CreateShaders(factory);
            CreatePipeline(factory);
        }

        /// <summary>
        /// 更新黑洞参数（在 Module 的 OnUpdate 里调）
        /// </summary>
        public void UpdateParams(GraphicsDevice gd, in BlackHoleParams p)
        {
            if (_paramsUniform == null) return;
            var temp = p;
            _paramsUniform.Update(gd, ref temp);
        }

        public override void Draw(CommandList commandList)
        {
            if (_vertexBuffer == null || _pipeline == null) return;

            commandList.SetPipeline(_pipeline);
            commandList.SetVertexBuffer(0, _vertexBuffer);

            if (_indexBuffer != null)
            {
                commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            }

            if (_resourceSet != null)
            {
                commandList.SetGraphicsResourceSet(0, _resourceSet);
            }

            if (_indexBuffer != null)
            {
                commandList.DrawIndexed(
                    indexCount: (uint)(_indexBuffer.SizeInBytes / sizeof(ushort)),
                    instanceCount: 1,
                    indexStart: 0,
                    vertexOffset: 0,
                    instanceStart: 0);
            }
            else
            {
                // 理论上不会走这里
                uint vertexSize = 0;
                foreach (var e in VertexLayout.Elements)
                {
                    vertexSize += e.Format.GetSizeInBytes();
                }

                commandList.Draw((uint)(_vertexBuffer.SizeInBytes / vertexSize));
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _resourceSet?.Dispose();
            _resourceLayout?.Dispose();
            _paramsUniform?.Dispose();
        }
    }
}
