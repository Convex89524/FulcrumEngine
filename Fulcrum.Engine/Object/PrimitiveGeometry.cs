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
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid.Utilities;

namespace Fulcrum.Engine.Object
{
	public static class PrimitiveGeometry
	{
		// 创建立方体几何数据
		public static (VertexPositionNormalTexture[] vertices, ushort[] indices) CreateCube(float size = 1.0f)
		{
			float halfSize = size / 2.0f;

			// 定义8个顶点
			Vector3[] positions = new Vector3[]
			{
			new Vector3(-halfSize, -halfSize, -halfSize), // 0
            new Vector3( halfSize, -halfSize, -halfSize), // 1
            new Vector3( halfSize,  halfSize, -halfSize), // 2
            new Vector3(-halfSize,  halfSize, -halfSize), // 3
            new Vector3(-halfSize, -halfSize,  halfSize), // 4
            new Vector3( halfSize, -halfSize,  halfSize), // 5
            new Vector3( halfSize,  halfSize,  halfSize), // 6
            new Vector3(-halfSize,  halfSize,  halfSize)  // 7
			};

			// 定义6个面的法线
			Vector3[] normals = new Vector3[]
			{
			new Vector3(0, 0, -1), // 前
            new Vector3(0, 0, 1),  // 后
            new Vector3(-1, 0, 0), // 左
            new Vector3(1, 0, 0),  // 右
            new Vector3(0, -1, 0), // 下
            new Vector3(0, 1, 0)   // 上
			};

			// 定义纹理坐标
			Vector2[] texCoords = new Vector2[]
			{
			new Vector2(0, 0),
			new Vector2(1, 0),
			new Vector2(1, 1),
			new Vector2(0, 1)
			};

			// 创建顶点数组 (6个面 * 4个顶点 = 24个顶点)
			VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[24];
			ushort[] indices = new ushort[36]; // 6个面 * 2个三角形 * 3个顶点 = 36个索引

			int vertexIndex = 0;
			int indexIndex = 0;

			// 前脸
			AddFace(vertices, indices, ref vertexIndex, ref indexIndex,
					positions[0], positions[1], positions[2], positions[3],
					normals[0], texCoords[0], texCoords[1], texCoords[2], texCoords[3]);

			// 后脸
			AddFace(vertices, indices, ref vertexIndex, ref indexIndex,
					positions[5], positions[4], positions[7], positions[6],
					normals[1], texCoords[0], texCoords[1], texCoords[2], texCoords[3]);

			// 左脸
			AddFace(vertices, indices, ref vertexIndex, ref indexIndex,
					positions[4], positions[0], positions[3], positions[7],
					normals[2], texCoords[0], texCoords[1], texCoords[2], texCoords[3]);

			// 右脸
			AddFace(vertices, indices, ref vertexIndex, ref indexIndex,
					positions[1], positions[5], positions[6], positions[2],
					normals[3], texCoords[0], texCoords[1], texCoords[2], texCoords[3]);

			// 底脸
			AddFace(vertices, indices, ref vertexIndex, ref indexIndex,
					positions[4], positions[5], positions[1], positions[0],
					normals[4], texCoords[0], texCoords[1], texCoords[2], texCoords[3]);

			// 顶脸
			AddFace(vertices, indices, ref vertexIndex, ref indexIndex,
					positions[3], positions[2], positions[6], positions[7],
					normals[5], texCoords[0], texCoords[1], texCoords[2], texCoords[3]);

			return (vertices, indices);
		}

		// 创建平面几何数据
		public static (VertexPositionNormalTexture[] vertices, ushort[] indices) CreatePlane(float width, float height, int segmentsX = 1, int segmentsY = 1)
		{
			int vertexCount = (segmentsX + 1) * (segmentsY + 1);
			int indexCount = segmentsX * segmentsY * 6;

			VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[vertexCount];
			ushort[] indices = new ushort[indexCount];

			float stepX = width / segmentsX;
			float stepY = height / segmentsY;
			float halfWidth = width / 2.0f;
			float halfHeight = height / 2.0f;

			Vector3 normal = new Vector3(0, 1, 0);

			// 创建顶点
			for (int y = 0; y <= segmentsY; y++)
			{
				for (int x = 0; x <= segmentsX; x++)
				{
					int index = y * (segmentsX + 1) + x;
					float posX = x * stepX - halfWidth;
					float posZ = y * stepY - halfHeight;

					vertices[index] = new VertexPositionNormalTexture(
						new Vector3(posX, 0, posZ),
						normal,
						new Vector2((float)x / segmentsX, (float)y / segmentsY)
					);
				}
			}

			// 创建索引
			int indicesIndex = 0;
			for (int y = 0; y < segmentsY; y++)
			{
				for (int x = 0; x < segmentsX; x++)
				{
					int topLeft = y * (segmentsX + 1) + x;
					int topRight = topLeft + 1;
					int bottomLeft = (y + 1) * (segmentsX + 1) + x;
					int bottomRight = bottomLeft + 1;

					indices[indicesIndex++] = (ushort)topLeft;
					indices[indicesIndex++] = (ushort)bottomLeft;
					indices[indicesIndex++] = (ushort)topRight;

					indices[indicesIndex++] = (ushort)topRight;
					indices[indicesIndex++] = (ushort)bottomLeft;
					indices[indicesIndex++] = (ushort)bottomRight;
				}
			}

			return (vertices, indices);
		}

		// 创建球体几何数据
		public static (VertexPositionNormalTexture[] vertices, ushort[] indices) CreateSphere(float radius, int segments = 16, int rings = 16)
		{
			int vertexCount = (rings + 1) * (segments + 1);
			int indexCount = rings * segments * 6;

			VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[vertexCount];
			ushort[] indices = new ushort[indexCount];

			// 创建顶点
			for (int ring = 0; ring <= rings; ring++)
			{
				float v = (float)ring / rings;
				float phi = v * MathF.PI;

				for (int segment = 0; segment <= segments; segment++)
				{
					float u = (float)segment / segments;
					float theta = u * MathF.PI * 2.0f;

					float x = MathF.Cos(theta) * MathF.Sin(phi);
					float y = MathF.Cos(phi);
					float z = MathF.Sin(theta) * MathF.Sin(phi);

					Vector3 position = new Vector3(x, y, z) * radius;
					Vector3 normal = Vector3.Normalize(new Vector3(x, y, z));
					Vector2 texCoord = new Vector2(u, v);

					int index = ring * (segments + 1) + segment;
					vertices[index] = new VertexPositionNormalTexture(position, normal, texCoord);
				}
			}

			// 创建索引
			int indicesIndex = 0;
			for (int ring = 0; ring < rings; ring++)
			{
				for (int segment = 0; segment < segments; segment++)
				{
					int current = ring * (segments + 1) + segment;
					int next = current + segments + 1;

					indices[indicesIndex++] = (ushort)current;
					indices[indicesIndex++] = (ushort)next;
					indices[indicesIndex++] = (ushort)(current + 1);

					indices[indicesIndex++] = (ushort)(current + 1);
					indices[indicesIndex++] = (ushort)next;
					indices[indicesIndex++] = (ushort)(next + 1);
				}
			}

			return (vertices, indices);
		}

		private static void AddFace(
			VertexPositionNormalTexture[] vertices, ushort[] indices,
			ref int vertexIndex, ref int indexIndex,
			Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4,
			Vector3 normal, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
		{
			ushort startIndex = (ushort)vertexIndex;

			vertices[vertexIndex++] = new VertexPositionNormalTexture(p1, normal, uv1);
			vertices[vertexIndex++] = new VertexPositionNormalTexture(p2, normal, uv2);
			vertices[vertexIndex++] = new VertexPositionNormalTexture(p3, normal, uv3);
			vertices[vertexIndex++] = new VertexPositionNormalTexture(p4, normal, uv4);

			indices[indexIndex++] = startIndex;
			indices[indexIndex++] = (ushort)(startIndex + 1);
			indices[indexIndex++] = (ushort)(startIndex + 2);

			indices[indexIndex++] = startIndex;
			indices[indexIndex++] = (ushort)(startIndex + 2);
			indices[indexIndex++] = (ushort)(startIndex + 3);
		}
	}

}
