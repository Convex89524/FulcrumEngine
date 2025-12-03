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
using System.Runtime.InteropServices;
using Veldrid;

namespace Fulcrum.Engine.Render
{
    public static class VertexElementFormatExtensions
    {
        public static uint GetSizeInBytes(this VertexElementFormat format)
        {
            switch (format)
            {
                case VertexElementFormat.Float1: return 4;
                case VertexElementFormat.Float2: return 8;
                case VertexElementFormat.Float3: return 12;
                case VertexElementFormat.Float4: return 16;
                case VertexElementFormat.Byte2_Norm: return 2;
                case VertexElementFormat.Byte2: return 2;
                case VertexElementFormat.Byte4_Norm: return 4;
                case VertexElementFormat.Byte4: return 4;
                case VertexElementFormat.SByte2_Norm: return 2;
                case VertexElementFormat.SByte2: return 2;
                case VertexElementFormat.SByte4_Norm: return 4;
                case VertexElementFormat.SByte4: return 4;
                case VertexElementFormat.Short2_Norm: return 4;
                case VertexElementFormat.Short2: return 4;
                case VertexElementFormat.Short4_Norm: return 8;
                case VertexElementFormat.Short4: return 8;
                case VertexElementFormat.UShort2_Norm: return 4;
                case VertexElementFormat.UShort2: return 4;
                case VertexElementFormat.UShort4_Norm: return 8;
                case VertexElementFormat.UShort4: return 8;
                case VertexElementFormat.UInt1: return 4;
                case VertexElementFormat.UInt2: return 8;
                case VertexElementFormat.UInt3: return 12;
                case VertexElementFormat.UInt4: return 16;
                case VertexElementFormat.Int1: return 4;
                case VertexElementFormat.Int2: return 8;
                case VertexElementFormat.Int3: return 12;
                case VertexElementFormat.Int4: return 16;
                case VertexElementFormat.Half1: return 2;
                case VertexElementFormat.Half2: return 4;
                case VertexElementFormat.Half4: return 8;
                default: throw new ArgumentException($"Unsupported format: {format}");
            }
        }
    }

    // 3D顶点结构体
    public struct VertexPositionNormalTexture
    {
        public const uint SizeInBytes = 32;
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;

        public VertexPositionNormalTexture(Vector3 position, Vector3 normal, Vector2 texCoord)
        {
            Position = position;
            Normal = normal;
            TexCoord = texCoord;
        }

        public static VertexLayoutDescription Layout => new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
    }

    // 顶点结构体（2D，带色）
    public struct VertexPositionColor
    {
        public const uint SizeInBytes = 24;
        public Vector2 Position;
        public RgbaFloat Color;

        public VertexPositionColor(Vector2 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }

        public static VertexLayoutDescription Layout => new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
    }

    // 带纹理的2D顶点结构体
    public struct VertexPositionTexture
    {
        public const uint SizeInBytes = 20;
        public Vector2 Position;
        public Vector2 TexCoord;

        public VertexPositionTexture(Vector2 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }

        public static VertexLayoutDescription Layout => new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));
    }

    // 3D变换矩阵结构
    public struct Matrix4x4Uniform
    {
        public System.Numerics.Matrix4x4 Model;
        public System.Numerics.Matrix4x4 View;
        public System.Numerics.Matrix4x4 Projection;

        public static uint SizeInBytes => (uint)Marshal.SizeOf<Matrix4x4Uniform>();
    }

    // Uniform缓冲区资源
    public class UniformBufferResource<T> : RenderResource where T : unmanaged
    {
        private DeviceBuffer _buffer;

        public UniformBufferResource(string name)
        {
            Name = name;
        }

        public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
        {
            _buffer = factory.CreateBuffer(new BufferDescription(SizeOf<T>(), BufferUsage.UniformBuffer));
        }

        public void Update(GraphicsDevice gd, ref T data)
        {
            gd.UpdateBuffer(_buffer, 0, ref data);
        }

        public DeviceBuffer GetBuffer() => _buffer;

        public override void Dispose()
        {
            _buffer?.Dispose();
        }

        private static uint SizeOf<T>() where T : unmanaged
        {
            return (uint)Marshal.SizeOf<T>();
        }
    }
}