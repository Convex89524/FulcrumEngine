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
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Linq;
using Veldrid;

namespace Fulcrum.Engine.Render
{
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
}