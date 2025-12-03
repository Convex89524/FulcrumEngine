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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CMLS.CLogger;
using Fulcrum.Common;
using Veldrid;
using Veldrid.SPIRV;

namespace Fulcrum.Engine.Render
{
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
}