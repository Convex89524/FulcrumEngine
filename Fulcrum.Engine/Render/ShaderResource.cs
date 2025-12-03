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
using Veldrid;
using Veldrid.SPIRV;

namespace Fulcrum.Engine.Render
{
    // 着色器资源
    public class ShaderResource : RenderResource
    {
        private Shader[] _shaders;
        private string _vertexShaderPath;
        private string _fragmentShaderPath;
        private byte[] _vertexShaderBytes;
        private byte[] _fragmentShaderBytes;

        private RendererConfig _config;

        public string VertexShaderPath => _vertexShaderPath;
        public string FragmentShaderPath => _fragmentShaderPath;

        public ShaderResource(string name, string vertexShaderPath, string fragmentShaderPath, RendererConfig config = null)
        {
            Name = name;
            _vertexShaderPath = vertexShaderPath;
            _fragmentShaderPath = fragmentShaderPath;
            _config = config;
        }

        public ShaderResource(string name, byte[] vertexShaderBytes, byte[] fragmentShaderBytes, RendererConfig config = null)
        {
            Name = name;
            _vertexShaderBytes = vertexShaderBytes;
            _fragmentShaderBytes = fragmentShaderBytes;
            _config = config;
        }

        public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
        {
            if (_vertexShaderBytes != null && _fragmentShaderBytes != null)
            {
                _shaders = factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, _vertexShaderBytes, "main"),
                    new ShaderDescription(ShaderStages.Fragment, _fragmentShaderBytes, "main")
                );
            }
            else if (!string.IsNullOrEmpty(_vertexShaderPath) && !string.IsNullOrEmpty(_fragmentShaderPath))
            {
                if (File.Exists(_vertexShaderPath) && File.Exists(_fragmentShaderPath))
                {
                    _shaders = factory.CreateFromSpirv(
                        new ShaderDescription(ShaderStages.Vertex, File.ReadAllBytes(_vertexShaderPath), "main"),
                        new ShaderDescription(ShaderStages.Fragment, File.ReadAllBytes(_fragmentShaderPath), "main")
                    );
                }
                else
                {
                    throw new FileNotFoundException($"Shader files not found: {_vertexShaderPath} or {_fragmentShaderPath}");
                }
            }
            else
            {
                throw new InvalidOperationException("No shader source provided. Set either shader paths or shader bytes.");
            }
        }
        public Shader[] GetShaders() => _shaders;

        public override void Dispose()
        {
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