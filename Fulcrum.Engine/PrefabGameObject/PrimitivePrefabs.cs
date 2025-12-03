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
using Fulcrum.Engine.Scene;
using Fulcrum.Engine.GameObjectComponent;
using Fulcrum.Engine.Render;

namespace Fulcrum.Engine.PrefabGameObject
{
    public class Cube : GameObject
    {
        public Vector3 Size
        {
            get => _size;
            set
            {
                _size = value;
                UpdateMesh();
            }
        }

        private Vector3 _size;
        private MeshRenderer _meshRenderer;

        public Cube(string name = "Cube", Vector3? size = null) : base(name)
        {
            _size = size ?? new Vector3(1, 1, 1);
            Build();
        }
        
        public void SetSize(Vector3 size)
        {
            Size = size;
        }

        private void Build()
        {
            _meshRenderer = AddComponent<MeshRenderer>();
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            float hx = _size.X * 0.5f;
            float hy = _size.Y * 0.5f;
            float hz = _size.Z * 0.5f;

            var v = new[]
            {
                // 前面
                V(-hx, -hy,  hz,  0, 0,  0, 0),
                V( hx, -hy,  hz,  1, 0,  1, 0),
                V( hx,  hy,  hz,  1, 1,  1, 1),
                V(-hx,  hy,  hz,  0, 1,  0, 1),

                // 后面
                V( hx, -hy, -hz,  0, 0,  0, 0),
                V(-hx, -hy, -hz,  1, 0,  1, 0),
                V(-hx,  hy, -hz,  1, 1,  1, 1),
                V( hx,  hy, -hz,  0, 1,  0, 1),

                // 左
                V(-hx, -hy, -hz,  0, 0,  0, 0),
                V(-hx, -hy,  hz,  1, 0,  1, 0),
                V(-hx,  hy,  hz,  1, 1,  1, 1),
                V(-hx,  hy, -hz,  0, 1,  0, 1),

                // 右
                V( hx, -hy,  hz,  0, 0,  0, 0),
                V( hx, -hy, -hz,  1, 0,  1, 0),
                V( hx,  hy, -hz,  1, 1,  1, 1),
                V( hx,  hy,  hz,  0, 1,  0, 1),

                // 上
                V(-hx,  hy,  hz,  0, 0,  0, 0),
                V( hx,  hy,  hz,  1, 0,  1, 0),
                V( hx,  hy, -hz,  1, 1,  1, 1),
                V(-hx,  hy, -hz,  0, 1,  0, 1),

                // 下
                V(-hx, -hy, -hz,  0, 0,  0, 0),
                V( hx, -hy, -hz,  1, 0,  1, 0),
                V( hx, -hy,  hz,  1, 1,  1, 1),
                V(-hx, -hy,  hz,  0, 1,  0, 1),
            };

            var idx = new ushort[]
            {
                0,1,2, 0,2,3,       // 前
                4,5,6, 4,6,7,       // 后
                8,9,10, 8,10,11,    // 左
                12,13,14, 12,14,15, // 右
                16,17,18, 16,18,19, // 上
                20,21,22, 20,22,23  // 下
            };

            _meshRenderer.SetMesh(v, idx);
        }

        private static VertexPositionNormalTexture V(
            float x, float y, float z,
            float u, float v,
            float nx, float ny)
        {
            return new VertexPositionNormalTexture
            {
                Position = new Vector3(x, y, z),
                Normal = new Vector3(nx, ny, 0),
                TexCoord = new Vector2(u, v)
            };
        }
    }
    
    /// <summary>
    /// 球体预制件
    /// </summary>
    public class Sphere : GameObject
    {
        private MeshRenderer _meshRenderer;

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = MathF.Max(0.0001f, value);
                UpdateMesh();
            }
        }

        public int LatitudeSegments
        {
            get => _latSegments;
            set
            {
                _latSegments = Math.Clamp(value, 3, 128);
                UpdateMesh();
            }
        }

        public int LongitudeSegments
        {
            get => _lonSegments;
            set
            {
                _lonSegments = Math.Clamp(value, 4, 256);
                UpdateMesh();
            }
        }

        private float _radius;
        private int _latSegments;
        private int _lonSegments;

        public Sphere(string name = "Sphere", float radius = 0.5f, int latSegments = 16, int lonSegments = 24)
            : base(name)
        {
            _radius      = radius;
            _latSegments = latSegments;
            _lonSegments = lonSegments;

            _meshRenderer = AddComponent<MeshRenderer>();
            UpdateMesh();
        }
        
        public void SetSize(Vector3 size)
        {
            float diameter = (size.X + size.Y + size.Z) / 3.0f;
            Radius = diameter * 0.5f;
        }

        private void UpdateMesh()
        {
            var vertices = new List<VertexPositionNormalTexture>();
            var indices  = new List<ushort>();

            for (int lat = 0; lat <= _latSegments; lat++)
            {
                float v = (float)lat / _latSegments;
                float theta = v * MathF.PI; // 0..PI

                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                for (int lon = 0; lon <= _lonSegments; lon++)
                {
                    float u = (float)lon / _lonSegments;
                    float phi = u * MathF.PI * 2.0f; // 0..2PI

                    float sinPhi = MathF.Sin(phi);
                    float cosPhi = MathF.Cos(phi);

                    Vector3 normal = new Vector3(
                        sinTheta * cosPhi,
                        cosTheta,
                        sinTheta * sinPhi);

                    Vector3 pos = normal * _radius;
                    Vector2 uv  = new Vector2(u, 1.0f - v);

                    vertices.Add(new VertexPositionNormalTexture
                    {
                        Position = pos,
                        Normal   = Vector3.Normalize(normal),
                        TexCoord = uv
                    });
                }
            }

            int stride = _lonSegments + 1;

            for (int lat = 0; lat < _latSegments; lat++)
            {
                for (int lon = 0; lon < _lonSegments; lon++)
                {
                    int i0 = lat * stride + lon;
                    int i1 = i0 + 1;
                    int i2 = (lat + 1) * stride + lon;
                    int i3 = i2 + 1;

                    indices.Add((ushort)i0);
                    indices.Add((ushort)i2);
                    indices.Add((ushort)i1);

                    indices.Add((ushort)i1);
                    indices.Add((ushort)i2);
                    indices.Add((ushort)i3);
                }
            }

            _meshRenderer.SetMesh(vertices.ToArray(), indices.ToArray());
        }
    }

    /// <summary>
    /// 圆柱体预制件（带上下盖）
    /// </summary>
    public class Cylinder : GameObject
    {
        private MeshRenderer _meshRenderer;

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = MathF.Max(0.0001f, value);
                UpdateMesh();
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                UpdateMesh();
            }
        }

        public int Segments
        {
            get => _segments;
            set
            {
                _segments = Math.Clamp(value, 3, 128);
                UpdateMesh();
            }
        }

        private float _radius;
        private float _height;
        private int _segments;

        public Cylinder(string name = "Cylinder", float radius = 0.5f, float height = 1.0f, int segments = 24)
            : base(name)
        {
            _radius   = radius;
            _height   = height;
            _segments = segments;

            _meshRenderer = AddComponent<MeshRenderer>();
            UpdateMesh();
        }
        
        public void SetSize(Vector3 size)
        {
            Radius = MathF.Max(size.X, size.Z) * 0.5f; 
            Height = size.Y;
        }

        private void UpdateMesh()
        {
            var vertices = new List<VertexPositionNormalTexture>();
            var indices  = new List<ushort>();

            float halfH = _height * 0.5f;

            // 侧面
            for (int i = 0; i <= _segments; i++)
            {
                float u = (float)i / _segments;
                float angle = u * MathF.PI * 2.0f;
                float c = MathF.Cos(angle);
                float s = MathF.Sin(angle);

                Vector3 normal = new Vector3(c, 0, s);

                Vector3 posBottom = new Vector3(c * _radius, -halfH, s * _radius);
                Vector3 posTop    = new Vector3(c * _radius,  halfH, s * _radius);

                vertices.Add(new VertexPositionNormalTexture
                {
                    Position = posBottom,
                    Normal   = Vector3.Normalize(normal),
                    TexCoord = new Vector2(u, 1)
                });

                vertices.Add(new VertexPositionNormalTexture
                {
                    Position = posTop,
                    Normal   = Vector3.Normalize(normal),
                    TexCoord = new Vector2(u, 0)
                });
            }

            // 侧面索引
            for (int i = 0; i < _segments; i++)
            {
                ushort i0 = (ushort)(i * 2);
                ushort i1 = (ushort)(i * 2 + 1);
                ushort i2 = (ushort)((i + 1) * 2);
                ushort i3 = (ushort)((i + 1) * 2 + 1);

                indices.Add(i0);
                indices.Add(i2);
                indices.Add(i1);

                indices.Add(i1);
                indices.Add(i2);
                indices.Add(i3);
            }

            int baseIndex = vertices.Count;

            // 顶盖中心
            vertices.Add(new VertexPositionNormalTexture
            {
                Position = new Vector3(0, halfH, 0),
                Normal   = Vector3.UnitY,
                TexCoord = new Vector2(0.5f, 0.5f)
            });

            // 顶盖边
            for (int i = 0; i <= _segments; i++)
            {
                float u = (float)i / _segments;
                float angle = u * MathF.PI * 2.0f;
                float c = MathF.Cos(angle);
                float s = MathF.Sin(angle);

                Vector3 pos = new Vector3(c * _radius, halfH, s * _radius);
                vertices.Add(new VertexPositionNormalTexture
                {
                    Position = pos,
                    Normal   = Vector3.UnitY,
                    TexCoord = new Vector2(c * 0.5f + 0.5f, s * 0.5f + 0.5f)
                });
            }

            // 顶盖索引
            for (int i = 0; i < _segments; i++)
            {
                ushort center = (ushort)baseIndex;
                ushort i1 = (ushort)(baseIndex + i + 1);
                ushort i2 = (ushort)(baseIndex + i + 2);

                indices.Add(center);
                indices.Add(i1);
                indices.Add(i2);
            }

            baseIndex = vertices.Count;

            // 底盖中心
            vertices.Add(new VertexPositionNormalTexture
            {
                Position = new Vector3(0, -halfH, 0),
                Normal   = -Vector3.UnitY,
                TexCoord = new Vector2(0.5f, 0.5f)
            });

            // 底盖边
            for (int i = 0; i <= _segments; i++)
            {
                float u = (float)i / _segments;
                float angle = u * MathF.PI * 2.0f;
                float c = MathF.Cos(angle);
                float s = MathF.Sin(angle);

                Vector3 pos = new Vector3(c * _radius, -halfH, s * _radius);
                vertices.Add(new VertexPositionNormalTexture
                {
                    Position = pos,
                    Normal   = -Vector3.UnitY,
                    TexCoord = new Vector2(c * 0.5f + 0.5f, s * 0.5f + 0.5f)
                });
            }

            // 底盖索引
            for (int i = 0; i < _segments; i++)
            {
                ushort center = (ushort)baseIndex;
                ushort i1 = (ushort)(baseIndex + i + 1);
                ushort i2 = (ushort)(baseIndex + i + 2);

                indices.Add(center);
                indices.Add(i2);
                indices.Add(i1);
            }

            _meshRenderer.SetMesh(vertices.ToArray(), indices.ToArray());
        }
    }

    /// <summary>
    /// 三角柱体
    /// </summary>
    public class TriangularPrism : GameObject
    {
        private MeshRenderer _meshRenderer;

        /// <summary>底面宽度 (X)</summary>
        public float Width
        {
            get => _width;
            set { _width = value; UpdateMesh(); }
        }

        /// <summary>高度 (Y)</summary>
        public float Height
        {
            get => _height;
            set { _height = value; UpdateMesh(); }
        }

        /// <summary>深度 (Z)</summary>
        public float Depth
        {
            get => _depth;
            set { _depth = value; UpdateMesh(); }
        }

        private float _width;
        private float _height;
        private float _depth;

        public TriangularPrism(string name = "TriangularPrism",
            float width = 1.0f, float height = 1.0f, float depth = 1.0f)
            : base(name)
        {
            _width  = width;
            _height = height;
            _depth  = depth;

            _meshRenderer = AddComponent<MeshRenderer>();
            UpdateMesh();
        }
        
        public void SetSize(Vector3 size)
        {
            Width  = size.X;
            Height = size.Y;
            Depth  = size.Z;
        }

        private void UpdateMesh()
        {
            float hx = _width  * 0.5f;
            float hy = _height;
            float hz = _depth  * 0.5f;

            // 底三角 (y=0)
            Vector3 b0 = new Vector3(-hx, 0, -hz);
            Vector3 b1 = new Vector3( hx, 0, -hz);
            Vector3 b2 = new Vector3( 0f, 0,  hz);

            // 顶三角 (y=hy)
            Vector3 t0 = new Vector3(-hx, hy, -hz);
            Vector3 t1 = new Vector3( hx, hy, -hz);
            Vector3 t2 = new Vector3( 0f, hy,  hz);

            var vertices = new List<VertexPositionNormalTexture>();
            var indices  = new List<ushort>();

            // 底面三角
            {
                Vector3 n = ComputeNormal(b2, b1, b0);

                int start = vertices.Count;
                vertices.Add(V(b2, n, new Vector2(0.5f, 1)));
                vertices.Add(V(b1, n, new Vector2(1, 0)));
                vertices.Add(V(b0, n, new Vector2(0, 0)));

                indices.Add((ushort)start);
                indices.Add((ushort)(start + 1));
                indices.Add((ushort)(start + 2));
            }

            // 顶面三角
            {
                Vector3 n = ComputeNormal(t2, t1, t0);
                int start = vertices.Count;
                vertices.Add(V(t0, n, new Vector2(0, 0)));
                vertices.Add(V(t1, n, new Vector2(1, 0)));
                vertices.Add(V(t2, n, new Vector2(0.5f, 1)));

                indices.Add((ushort)start);
                indices.Add((ushort)(start + 1));
                indices.Add((ushort)(start + 2));
            }

            // 侧面1: b0-b1-t1-t0
            {
                Vector3 v0 = b0;
                Vector3 v1 = b1;
                Vector3 v2 = t1;
                Vector3 v3 = t0;
                Vector3 n = ComputeNormal(v0, v1, v2);
                int start = vertices.Count;
                vertices.Add(V(v0, n, new Vector2(0, 0)));
                vertices.Add(V(v1, n, new Vector2(1, 0)));
                vertices.Add(V(v2, n, new Vector2(1, 1)));
                vertices.Add(V(v3, n, new Vector2(0, 1)));

                indices.Add((ushort)start);
                indices.Add((ushort)(start + 1));
                indices.Add((ushort)(start + 2));
                indices.Add((ushort)start);
                indices.Add((ushort)(start + 2));
                indices.Add((ushort)(start + 3));
            }

            // 侧面2: b1-b2-t2-t1
            {
                Vector3 v0 = b1;
                Vector3 v1 = b2;
                Vector3 v2 = t2;
                Vector3 v3 = t1;
                Vector3 n = ComputeNormal(v0, v1, v2);
                int start = vertices.Count;
                vertices.Add(V(v0, n, new Vector2(0, 0)));
                vertices.Add(V(v1, n, new Vector2(1, 0)));
                vertices.Add(V(v2, n, new Vector2(1, 1)));
                vertices.Add(V(v3, n, new Vector2(0, 1)));

                indices.Add((ushort)start);
                indices.Add((ushort)(start + 1));
                indices.Add((ushort)(start + 2));
                indices.Add((ushort)start);
                indices.Add((ushort)(start + 2));
                indices.Add((ushort)(start + 3));
            }

            // 侧面3: b2-b0-t0-t2
            {
                Vector3 v0 = b2;
                Vector3 v1 = b0;
                Vector3 v2 = t0;
                Vector3 v3 = t2;
                Vector3 n = ComputeNormal(v0, v1, v2);
                int start = vertices.Count;
                vertices.Add(V(v0, n, new Vector2(0, 0)));
                vertices.Add(V(v1, n, new Vector2(1, 0)));
                vertices.Add(V(v2, n, new Vector2(1, 1)));
                vertices.Add(V(v3, n, new Vector2(0, 1)));

                indices.Add((ushort)start);
                indices.Add((ushort)(start + 1));
                indices.Add((ushort)(start + 2));
                indices.Add((ushort)start);
                indices.Add((ushort)(start + 2));
                indices.Add((ushort)(start + 3));
            }

            _meshRenderer.SetMesh(vertices.ToArray(), indices.ToArray());
        }

        private static Vector3 ComputeNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var n = Vector3.Cross(ab, ac);
            if (n.LengthSquared() < 1e-8f) return Vector3.UnitY;
            return Vector3.Normalize(n);
        }

        private static VertexPositionNormalTexture V(Vector3 pos, Vector3 n, Vector2 uv)
        {
            return new VertexPositionNormalTexture
            {
                Position = pos,
                Normal   = Vector3.Normalize(n),
                TexCoord = uv
            };
        }
    }

    /// <summary>
    /// 楔体预制件
    /// </summary>
    public class Wedge : GameObject
    {
        private MeshRenderer _meshRenderer;

        public Vector3 Size
        {
            get => _size;
            set { _size = value; UpdateMesh(); }
        }

        private Vector3 _size;

        public Wedge(string name = "Wedge", Vector3? size = null) : base(name)
        {
            _size = size ?? new Vector3(1, 1, 1);
            _meshRenderer = AddComponent<MeshRenderer>();
            UpdateMesh();
        }
        
        public void SetSize(Vector3 size)
        {
            Size = size;
        }

        private void UpdateMesh()
        {
            float hx = _size.X * 0.5f;
            float hy = _size.Y * 0.5f;
            float hz = _size.Z * 0.5f;

            Vector3 b0 = new Vector3(-hx, -hy, -hz);
            Vector3 b1 = new Vector3( hx, -hy, -hz);
            Vector3 b2 = new Vector3( hx, -hy,  hz);
            Vector3 b3 = new Vector3(-hx, -hy,  hz);

            Vector3 t0 = new Vector3(-hx,  hy,  hz);
            Vector3 t1 = new Vector3( hx,  hy,  hz);

            var vertices = new List<VertexPositionNormalTexture>();
            var indices  = new List<ushort>();

            {
                Vector3 v0 = b3, v1 = b2, v2 = b1, v3 = b0;
                Vector3 n = ComputeNormal(v0, v1, v2);

                int s = vertices.Count;
                vertices.Add(V(v0, n, new Vector2(0, 1)));
                vertices.Add(V(v1, n, new Vector2(1, 1)));
                vertices.Add(V(v2, n, new Vector2(1, 0)));
                vertices.Add(V(v3, n, new Vector2(0, 0)));

                indices.Add((ushort)s); indices.Add((ushort)(s + 1)); indices.Add((ushort)(s + 2));
                indices.Add((ushort)s); indices.Add((ushort)(s + 2)); indices.Add((ushort)(s + 3));
            }

            {
                Vector3 v0 = t0, v1 = t1, v2 = b2, v3 = b3;
                Vector3 n = ComputeNormal(v0, v1, v2);

                int s = vertices.Count;
                vertices.Add(V(v0, n, new Vector2(0, 1)));
                vertices.Add(V(v1, n, new Vector2(1, 1)));
                vertices.Add(V(v2, n, new Vector2(1, 0)));
                vertices.Add(V(v3, n, new Vector2(0, 0)));

                indices.Add((ushort)s); indices.Add((ushort)(s + 1)); indices.Add((ushort)(s + 2));
                indices.Add((ushort)s); indices.Add((ushort)(s + 2)); indices.Add((ushort)(s + 3));
            }

            {
                Vector3 v0 = t0, v1 = b3, v2 = b0;
                Vector3 n = ComputeNormal(v0, v1, v2);

                int s = vertices.Count;
                vertices.Add(V(v0, n, new Vector2(0.5f, 1)));
                vertices.Add(V(v1, n, new Vector2(1, 0)));
                vertices.Add(V(v2, n, new Vector2(0, 0)));

                indices.Add((ushort)s); indices.Add((ushort)(s + 1)); indices.Add((ushort)(s + 2));
            }

            {
                Vector3 v0 = b1, v1 = b2, v2 = t1;
                Vector3 n = ComputeNormal(v0, v1, v2);

                int s = vertices.Count;
                vertices.Add(V(v0, n, new Vector2(0, 0)));
                vertices.Add(V(v1, n, new Vector2(1, 0)));
                vertices.Add(V(v2, n, new Vector2(1, 1)));

                indices.Add((ushort)s); indices.Add((ushort)(s + 1)); indices.Add((ushort)(s + 2));
            }

            {
                Vector3 v0 = b0, v1 = b1, v2 = t1, v3 = t0;
                Vector3 n = ComputeNormal(v0, v1, v2);

                int s = vertices.Count;
                vertices.Add(V(v0, n, new Vector2(0, 0)));
                vertices.Add(V(v1, n, new Vector2(1, 0)));
                vertices.Add(V(v2, n, new Vector2(1, 1)));
                vertices.Add(V(v3, n, new Vector2(0, 1)));

                indices.Add((ushort)s); indices.Add((ushort)(s + 1)); indices.Add((ushort)(s + 2));
                indices.Add((ushort)s); indices.Add((ushort)(s + 2)); indices.Add((ushort)(s + 3));
            }

            _meshRenderer.SetMesh(vertices.ToArray(), indices.ToArray());
        }
        
        private static Vector3 ComputeNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var n = Vector3.Cross(ab, ac);
            if (n.LengthSquared() < 1e-8f) return Vector3.UnitY;
            return Vector3.Normalize(n);
        }

        private static VertexPositionNormalTexture V(Vector3 pos, Vector3 n, Vector2 uv)
        {
            return new VertexPositionNormalTexture
            {
                Position = pos,
                Normal   = Vector3.Normalize(n),
                TexCoord = uv
            };
        }
    }
}
