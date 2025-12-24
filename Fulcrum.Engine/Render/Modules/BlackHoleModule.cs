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

using System;
using System.Numerics;
using Fulcrum.Engine.App;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Render.Renderables;
using Veldrid;

namespace Fulcrum.Engine.Render.Modules
{
    /// <summary>
    /// 全屏黑洞场景模块（事件视界 + 引力透镜 + 亮星空噪声背景）
    /// </summary>
    public sealed class BlackHoleModule : RenderApp.IModule
    {
        private BlackHoleRenderable _blackHole;
        private float _time;

        public void OnLoad(RenderApp app, RendererBase renderer)
        {
            // 基础全屏管线：无深度测试 + Alpha 混合
            var pipelineDesc = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: false,
                    depthWriteEnabled: false,
                    comparisonKind: ComparisonKind.Always),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.None,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = Array.Empty<ResourceLayout>()
            };

            _blackHole = new BlackHoleRenderable(
                name: "BlackHole",
                vertexLayout: VertexPositionTexture.Layout,
                pipelineDescription: pipelineDesc);
            
            _blackHole.SetShaderPaths(
                "shaders/blackhole.vert",
                "shaders/blackhole.frag");

            renderer.AddRenderable(_blackHole);
        }

        public void OnUpdate(RenderApp app, RendererBase renderer, float deltaTime)
        {
            if (_blackHole == null) return;

            _time += deltaTime;

            var p = new BlackHoleRenderable.BlackHoleParams
            {
                Time           = _time,

                // 黑洞半径（世界空间中的球半径，用于射线-球体求交）
                HorizonRadius  = 1.5f,

                // 引力透镜强度（越大越夸张）
                LensStrength   = 1.0f,

                // 星星整体亮度
                StarBrightness = 1.8f,

                // 噪声尺度：越大星星越密
                NoiseScale     = 40.0f,

                // 噪声强度（背景云气感）
                NoiseIntensity = 0.4f,

                // 曝光（整体映射到 0~1 的压缩强度）
                Exposure       = 1.0f,

                Pad0           = 0.0f
            };

            var cam = renderer.Camera;
            if (cam != null)
            {
                var forward = cam.Forward;
                var right   = cam.Right;
                var up      = cam.UpVector;

                // 实时获取窗口宽高比，避免窗口缩放时形状被拉伸
                float aspect = (float)renderer._config.WindowWidth / renderer._config.WindowHeight;

                p.CameraPos_Fov     = new Vector4(cam.Position, cam.FieldOfView);
                p.CameraForward_Pad = new Vector4(forward, 0f);
                p.CameraRight_Pad   = new Vector4(right,   0f);
                p.CameraUp_Aspect   = new Vector4(up,      aspect);
            }

            _blackHole.UpdateParams(renderer._graphicsDevice, in p);
        }

        public void Dispose()
        {
            _blackHole?.Dispose();
            _blackHole = null;
        }
    }
}
