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
using CMLS.CLogger;
using Fulcrum.Engine.App;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Sound;
using Fulcrum.Engine.Scene;
using BepuPhysics;
using BepuPhysics.Collidables;
using Veldrid;
using System.Runtime.InteropServices;
using Fulcrum.Engine;
using Veldrid.SPIRV;

namespace fulcrum.mainlogic
{
    public class CollisionDebugRenderer : ScriptBase
    {
        private Clogger _logger;
        private RenderApp _renderApp;
        private AudioEngine _audioEngine;
        
        // 线框渲染器
        private LineRenderer3D _lineRenderer;
        
        // 配置
        public RgbaFloat WireframeColor { get; set; } = new RgbaFloat(1.0f, 0.0f, 0.0f, 1.0f);
        public bool ShowStaticColliders { get; set; } = true;
        public bool ShowDynamicColliders { get; set; } = true;
        public bool ShowKinematicColliders { get; set; } = true;
        public bool Enabled { get; set; } = true;

        public override string ScriptId { get; protected set; } = "CollisionDebugRenderer";

        public override void OnLoad(Clogger logger, RenderApp renderApp, AudioEngine audioEngine)
        {
            _logger = logger;
            _renderApp = renderApp;
            _audioEngine = audioEngine;
            
            _logger.Info("Initializing Collision Debug Renderer...");
            
            InitializeLineRenderer();
            
            _logger.Info("Collision Debug Renderer initialized successfully");
        }

        private void InitializeLineRenderer()
        {
            try
            {
                _lineRenderer = new LineRenderer3D("CollisionDebugLines");
                _renderApp.Renderer.AddRenderable(_lineRenderer);
                _logger.Debug("Line renderer initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to initialize line renderer: {ex}");
            }
        }

        public override void OnUpdate(int currentTick, AudioEngine audioEngine)
        {
            if (!Enabled || _lineRenderer == null || !PhysicsWorld.Initialized)
                return;

            try
            {
                UpdateCollisionDebugData();
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error updating collision debug data: {ex.Message}");
            }
        }

        private void UpdateCollisionDebugData()
        {
            _lineRenderer.ClearLines();

            var simulation = PhysicsWorld.Simulation;
            if (simulation == null) return;

            for (int i = 0; i < simulation.Bodies.ActiveSet.Count; i++)
            {
                var bodyHandle = simulation.Bodies.ActiveSet.IndexToHandle[i];
                var bodyReference = simulation.Bodies.GetBodyReference(bodyHandle);
                
                if (!ShouldShowCollider(bodyReference))
                    continue;

                DrawColliderWireframe(bodyReference, simulation);
            }
        }

        private bool ShouldShowCollider(BodyReference bodyReference)
        {
            if (bodyReference.LocalInertia.InverseMass == 0)
                return ShowStaticColliders;
            
            return ShowDynamicColliders;
        }

        private void DrawColliderWireframe(BodyReference bodyReference, Simulation simulation)
        {
            var collidable = bodyReference.Collidable;
            var pose = bodyReference.Pose;

            try
            {
                var shapeIndex = collidable.Shape;
                if (!shapeIndex.Exists) return;

                DrawShape(shapeIndex, pose, simulation);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to draw collider wireframe: {ex.Message}");
                DrawSimpleBox(pose, new Vector3(1, 1, 1));
            }
        }

        private void DrawShape(TypedIndex shapeIndex, RigidPose pose, Simulation simulation)
        {
            try
            {
                switch (shapeIndex.Type)
                {
                    case Box.Id:
                    {
                        ref var box = ref simulation.Shapes.GetShape<Box>(shapeIndex.Index);
                        DrawBox(pose, box);
                        break;
                    }
                    case Sphere.Id:
                    {
                        ref var sphere = ref simulation.Shapes.GetShape<Sphere>(shapeIndex.Index);
                        DrawSphere(pose, sphere);
                        break;
                    }
                    case Cylinder.Id:
                    {
                        ref var cylinder = ref simulation.Shapes.GetShape<Cylinder>(shapeIndex.Index);
                        DrawCylinder(pose, cylinder);
                        break;
                    }
                    case Capsule.Id:
                    {
                        ref var capsule = ref simulation.Shapes.GetShape<Capsule>(shapeIndex.Index);
                        DrawCapsule(pose, capsule);
                        break;
                    }
                    default:
                        DrawSimpleBox(pose, new Vector3(1, 1, 1));
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to draw collider wireframe: {ex.Message}");
                DrawSimpleBox(pose, new Vector3(1, 1, 1));
            }
        }

        private void DrawBox(RigidPose pose, Box box)
        {
            var halfSize = new Vector3(box.Width, box.Height, box.Length) * 0.5f;
            DrawWireframeCube(pose, halfSize);
        }

        private void DrawSphere(RigidPose pose, Sphere sphere)
        {
            float radius = sphere.Radius;
            
            // 绘制三个方向的圆环
            const int segments = 12;
            DrawWireframeCircle(pose, radius, Vector3.UnitX, segments);
            DrawWireframeCircle(pose, radius, Vector3.UnitY, segments);
            DrawWireframeCircle(pose, radius, Vector3.UnitZ, segments);
        }

        private void DrawCylinder(RigidPose pose, Cylinder cylinder)
        {
            float radius = cylinder.Radius;
            float halfHeight = cylinder.Length * 0.5f;
            const int segments = 12;

            // 顶部和底部的圆
            DrawWireframeCircle(pose, radius, Vector3.UnitY, segments, halfHeight);
            DrawWireframeCircle(pose, radius, Vector3.UnitY, segments, -halfHeight);

            // 侧面的四条线
            var rotationMatrix = Matrix4x4.CreateFromQuaternion(pose.Orientation);
            var position = pose.Position;

            for (int i = 0; i < 4; i++)
            {
                float angle = i * MathF.PI / 2;
                Vector3 localPoint = new Vector3(
                    MathF.Cos(angle) * radius,
                    0,
                    MathF.Sin(angle) * radius
                );

                Vector3 topPoint = position + Vector3.Transform(localPoint + new Vector3(0, halfHeight, 0), rotationMatrix);
                Vector3 bottomPoint = position + Vector3.Transform(localPoint + new Vector3(0, -halfHeight, 0), rotationMatrix);

                _lineRenderer.AddLine(topPoint, bottomPoint, WireframeColor);
            }
        }

        private void DrawCapsule(RigidPose pose, Capsule capsule)
        {
            float radius = capsule.Radius;
            float halfLength = capsule.Length * 0.5f;
            const int segments = 12;

            // 绘制圆柱部分
            DrawWireframeCircle(pose, radius, Vector3.UnitY, segments, halfLength);
            DrawWireframeCircle(pose, radius, Vector3.UnitY, segments, -halfLength);

            // 绘制侧面线
            var rotationMatrix = Matrix4x4.CreateFromQuaternion(pose.Orientation);
            var position = pose.Position;

            for (int i = 0; i < 4; i++)
            {
                float angle = i * MathF.PI / 2;
                Vector3 localPoint = new Vector3(
                    MathF.Cos(angle) * radius,
                    0,
                    MathF.Sin(angle) * radius
                );

                Vector3 topPoint = position + Vector3.Transform(localPoint + new Vector3(0, halfLength, 0), rotationMatrix);
                Vector3 bottomPoint = position + Vector3.Transform(localPoint + new Vector3(0, -halfLength, 0), rotationMatrix);

                _lineRenderer.AddLine(topPoint, bottomPoint, WireframeColor);
            }
        }

        private void DrawSimpleBox(RigidPose pose, Vector3 halfSize)
        {
            DrawWireframeCube(pose, halfSize);
        }

        private void DrawWireframeCube(RigidPose pose, Vector3 halfSize)
        {
            var rotationMatrix = Matrix4x4.CreateFromQuaternion(pose.Orientation);
            var position = pose.Position;

            // 立方体的8个顶点
            Vector3[] vertices = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3 localVertex = new Vector3(
                    (i & 1) == 0 ? -halfSize.X : halfSize.X,
                    (i & 2) == 0 ? -halfSize.Y : halfSize.Y,
                    (i & 4) == 0 ? -halfSize.Z : halfSize.Z
                );
                vertices[i] = position + Vector3.Transform(localVertex, rotationMatrix);
            }

            // 绘制12条边
            int[,] edges = new int[12, 2] {
                {0,1}, {1,3}, {3,2}, {2,0}, // 底面
                {4,5}, {5,7}, {7,6}, {6,4}, // 顶面
                {0,4}, {1,5}, {2,6}, {3,7}  // 侧面
            };

            for (int i = 0; i < 12; i++)
            {
                _lineRenderer.AddLine(vertices[edges[i, 0]], vertices[edges[i, 1]], WireframeColor);
            }
        }

        private void DrawWireframeCircle(RigidPose pose, float radius, Vector3 axis, int segments, float yOffset = 0)
        {
            var rotationMatrix = Matrix4x4.CreateFromQuaternion(pose.Orientation);
            var position = pose.Position;

            Vector3 prevPoint = Vector3.Zero;
            Vector3 firstPoint = Vector3.Zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2 * MathF.PI / segments;
                
                Vector3 localPoint = axis switch
                {
                    _ when axis == Vector3.UnitY => new Vector3(
                        MathF.Cos(angle) * radius,
                        yOffset,
                        MathF.Sin(angle) * radius
                    ),
                    _ when axis == Vector3.UnitX => new Vector3(
                        yOffset,
                        MathF.Cos(angle) * radius,
                        MathF.Sin(angle) * radius
                    ),
                    _ => new Vector3(
                        MathF.Cos(angle) * radius,
                        MathF.Sin(angle) * radius,
                        yOffset
                    )
                };

                Vector3 worldPoint = position + Vector3.Transform(localPoint, rotationMatrix);

                if (i > 0)
                {
                    _lineRenderer.AddLine(prevPoint, worldPoint, WireframeColor);
                }
                else
                {
                    firstPoint = worldPoint;
                }

                prevPoint = worldPoint;
            }
        }

        public override void OnRenderFrame(RendererBase rendererBase)
        {
            // 渲染逻辑在LineRenderer3D中处理
        }

        public override void OnUninstall()
        {
            _logger.Info("Uninstalling Collision Debug Renderer...");
            
            try
            {
                if (_lineRenderer != null)
                {
                    _renderApp.Renderer.RemoveRenderable("CollisionDebugLines");
                    _lineRenderer.Dispose();
                    _lineRenderer = null;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during Collision Debug Renderer uninstall: {ex.Message}");
            }
            
            _logger.Info("Collision Debug Renderer uninstalled");
        }
    }

    // 3D线框渲染器
    public class LineRenderer3D : IRenderable
    {
        private GraphicsDevice _graphicsDevice;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private Pipeline _pipeline;
        private Shader[] _shaders;
        
        private List<LineVertex> _vertices = new List<LineVertex>();
        private List<ushort> _indices = new List<ushort>();
        
        public string Name { get; private set; }

        public LineRenderer3D(string name)
        {
            Name = name;
        }

        public void Initialize(GraphicsDevice gd, ResourceFactory factory)
        {
            _graphicsDevice = gd;
            CreateResources(factory);
        }

        private void CreateResources(ResourceFactory factory)
        {
            // 创建着色器
            string vertexShaderSource = @"
#version 450
layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
layout(set = 0, binding = 0) uniform ViewProjection {
    mat4 View;
    mat4 Projection;
};
void main() {
    gl_Position = Projection * View * vec4(Position, 1.0);
    fsin_Color = Color;
}";

            string fragmentShaderSource = @"
#version 450
layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;
void main() {
    fsout_Color = fsin_Color;
}";

            _shaders = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, 
                    System.Text.Encoding.UTF8.GetBytes(vertexShaderSource), "main"),
                new ShaderDescription(ShaderStages.Fragment, 
                    System.Text.Encoding.UTF8.GetBytes(fragmentShaderSource), "main")
            );

            // 创建流水线
            var pipelineDesc = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual
                ),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.None,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                ),
                PrimitiveTopology = PrimitiveTopology.LineList,
                ResourceLayouts = Array.Empty<ResourceLayout>(),
                ShaderSet = new ShaderSetDescription(
                    new[] { LineVertex.Layout },
                    _shaders
                ),
                Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription
            };

            _pipeline = factory.CreateGraphicsPipeline(pipelineDesc);
        }

        public void AddLine(Vector3 start, Vector3 end, RgbaFloat color)
        {
            ushort startIndex = (ushort)_vertices.Count;
            
            _vertices.Add(new LineVertex(start, color));
            _vertices.Add(new LineVertex(end, color));
            
            _indices.Add(startIndex);
            _indices.Add((ushort)(startIndex + 1));
        }

        public void ClearLines()
        {
            _vertices.Clear();
            _indices.Clear();
        }

        public void Draw(CommandList commandList)
        {
            if (_vertices.Count == 0 || _pipeline == null) 
                return;

            UpdateBuffers();

            commandList.SetPipeline(_pipeline);
            commandList.SetVertexBuffer(0, _vertexBuffer);
            commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            commandList.DrawIndexed((uint)_indices.Count, 1, 0, 0, 0);
        }

        private void UpdateBuffers()
        {
            if (_vertices.Count == 0) 
                return;

            uint vertexBufferSize = (uint)(_vertices.Count * LineVertex.SizeInBytes);
            uint indexBufferSize = (uint)(_indices.Count * sizeof(ushort));

            if (_vertexBuffer == null || _vertexBuffer.SizeInBytes < vertexBufferSize)
            {
                _vertexBuffer?.Dispose();
                _vertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription(vertexBufferSize, BufferUsage.VertexBuffer));
            }

            if (_indexBuffer == null || _indexBuffer.SizeInBytes < indexBufferSize)
            {
                _indexBuffer?.Dispose();
                _indexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription(indexBufferSize, BufferUsage.IndexBuffer));
            }

            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices.ToArray());
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices.ToArray());
        }

        public void Dispose()
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

    [StructLayout(LayoutKind.Sequential)]
    public struct LineVertex
    {
        public Vector3 Position;
        public RgbaFloat Color;

        public static uint SizeInBytes => (uint)Marshal.SizeOf<LineVertex>();

        public LineVertex(Vector3 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }

        public static VertexLayoutDescription Layout => new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
    }
}