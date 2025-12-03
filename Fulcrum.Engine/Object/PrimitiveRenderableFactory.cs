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

using Fulcrum.Engine.Render;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using static Fulcrum.Engine.Render.VertexElementFormatExtensions;

namespace Fulcrum.Engine.Object
{
	public static class PrimitiveRenderableFactory
	{
		// 创建立方体渲染器
		public static TexturedRenderable3D CreateCubeRenderable(
			string name,
			GraphicsDevice gd,
			ResourceFactory factory,
			TextureView textureView,
			float size = 1.0f,
			string vertexShaderPath = null,
			string fragmentShaderPath = null)
		{
			var (vertices, indices) = PrimitiveGeometry.CreateCube(size);

			var pipelineDesc = new GraphicsPipelineDescription
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

			var renderable = new TexturedRenderable3D(
				name,
				textureView,
				VertexPositionNormalTexture.Layout,
				pipelineDesc);

			if (!string.IsNullOrEmpty(vertexShaderPath) && !string.IsNullOrEmpty(fragmentShaderPath))
			{
				renderable.SetShaderPaths(vertexShaderPath, fragmentShaderPath);
			}

			renderable.Initialize(gd, factory);

			renderable.SetVertexData(vertices);
			renderable.SetIndexData(indices);

			return renderable;
		}

		// 创建平面渲染器
		public static TexturedRenderable3D CreatePlaneRenderable(
			string name,
			GraphicsDevice gd,
			ResourceFactory factory,
			TextureView textureView,
			float width = 1.0f,
			float height = 1.0f,
			int segmentsX = 1,
			int segmentsY = 1,
			string vertexShaderPath = null,
			string fragmentShaderPath = null)
		{
			// 获取平面几何数据
			var (vertices, indices) = PrimitiveGeometry.CreatePlane(width, height, segmentsX, segmentsY);

			// 创建管道描述
			var pipelineDesc = new GraphicsPipelineDescription
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

			// 创建渲染器
			var renderable = new TexturedRenderable3D(
				name,
				textureView,
				VertexPositionNormalTexture.Layout,
				pipelineDesc);

			renderable.Initialize(gd, factory);

			// 设置着色器路径（如果提供）
			if (!string.IsNullOrEmpty(vertexShaderPath) && !string.IsNullOrEmpty(fragmentShaderPath))
			{
				renderable.SetShaderPaths(vertexShaderPath, fragmentShaderPath);
			}

			// 设置几何数据
			renderable.SetVertexData(vertices);
			renderable.SetIndexData(indices);

			return renderable;
		}

		// 创建球体渲染器
		public static TexturedRenderable3D CreateSphereRenderable(
			string name,
			GraphicsDevice gd,
			ResourceFactory factory,
			TextureView textureView,
			float radius,
			int segments,
			int rings,
			string vertexShaderPath,
			string fragmentShaderPath,
			RendererConfig config)
		{
			// 获取球体几何数据
			var (vertices, indices) = PrimitiveGeometry.CreateSphere(radius, segments, rings);

			// 创建管道描述
			var pipelineDesc = new GraphicsPipelineDescription
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

			// 创建渲染器
			var renderable = new TexturedRenderable3D(
				name,
				textureView,
				VertexPositionNormalTexture.Layout,
				pipelineDesc
			);

			renderable.SetConfig(config);

			// 在初始化前设置着色器路径
			if (!string.IsNullOrEmpty(vertexShaderPath) && !string.IsNullOrEmpty(fragmentShaderPath))
			{
				renderable.SetShaderPaths(vertexShaderPath, fragmentShaderPath);
			}

			renderable.Initialize(gd, factory);

			// 设置几何数据
			renderable.SetVertexData(vertices);
			renderable.SetIndexData(indices);

			return renderable;
		}

		// 创建无纹理的几何体渲染器
		public static GeometryRenderable3D CreateUntexturedCubeRenderable(
			string name,
			GraphicsDevice gd,
			ResourceFactory factory,
			float size = 1.0f,
			string vertexShaderPath = null,
			string fragmentShaderPath = null)
		{
			// 获取立方体几何数据
			var (vertices, indices) = PrimitiveGeometry.CreateCube(size);

			// 创建管道描述
			var pipelineDesc = new GraphicsPipelineDescription
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

			// 创建渲染器
			var renderable = new GeometryRenderable3D(
				name,
				VertexPositionNormalTexture.Layout,
				pipelineDesc);

			renderable.Initialize(gd, factory);

			// 设置着色器路径（如果提供）
			if (!string.IsNullOrEmpty(vertexShaderPath) && !string.IsNullOrEmpty(fragmentShaderPath))
			{
				renderable.SetShaderPaths(vertexShaderPath, fragmentShaderPath);
			}

			// 设置几何数据
			renderable.SetVertexData(vertices);
			renderable.SetIndexData(indices);

			return renderable;
		}
	}
}
