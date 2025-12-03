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
using System.Runtime.InteropServices;
using Veldrid;
using Fulcrum.Engine.Render;
using CMLS.CLogger;

namespace Fulcrum.Engine.App.Render
{
    public sealed class SkyUniformBuffer<T> : IDisposable where T : unmanaged
    {
        private DeviceBuffer _buffer;
        private readonly string _name;

        public SkyUniformBuffer(string name)
        {
            _name = name;
        }

        public void Initialize(GraphicsDevice gd)
        {
            if (_buffer != null) return;
            _buffer = gd.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)Marshal.SizeOf<T>(), BufferUsage.UniformBuffer));
        }

        public void Update(GraphicsDevice gd, in T data)
        {
            gd.UpdateBuffer(_buffer, 0, data);
        }

        public DeviceBuffer Get() => _buffer;

        public void Dispose() => _buffer?.Dispose();
    }

    public sealed class ProceduralSkyRenderable : GeometryRenderable
    {
        private readonly Clogger LOGGER = LogManager.GetLogger("Sky");
        private DeviceBuffer _indexBuffer;
        private ResourceLayout _uboLayout;
        private ResourceSet _uboSet;

        // === UBO 数据结构 ===

        // binding 0
        [StructLayout(LayoutKind.Sequential)]
        public struct CameraUBO
        {
            public Matrix4x4 InvView;      // 64B
            public Matrix4x4 InvProj;      // 64B
            public Vector3   CameraPos;    // 12B
            private float    _pad0;        // 4B
        }

        // binding 1
        [StructLayout(LayoutKind.Sequential)]
        public struct TimeUBO
        {
            public float Time;             // 秒
            public float DeltaTime;        // 秒
            public float ViewportWidth;    // 像素
            public float ViewportHeight;   // 像素
        }

        // binding 2
        [StructLayout(LayoutKind.Sequential)]
        public struct SkyParamsUBO
        {
            public Vector3 SunDir;         // 太阳方向（世界/视空间，取决于你的 FS 约定）
            public float   Exposure;       // 曝光
            public Vector3 MoonDir;        // 月亮方向
            public float   StarIntensity;  // 星等强度
            public float   MilkyWay;       // 银河强度
            public float   Turbidity;      // 大气浑浊度
            public float   AtmosphereH;    // 大气层高度标尺
            private float  _pad1;          // 对齐
        }

        private SkyUniformBuffer<CameraUBO>    _uboCamera;
        private SkyUniformBuffer<TimeUBO>      _uboTime;
        private SkyUniformBuffer<SkyParamsUBO> _uboParams;

        private float _time;
        private bool  _initialized;

        private SkyParamsUBO _params = new SkyParamsUBO
        {
            SunDir        = Vector3.Normalize(new Vector3(0.3f, 0.6f, 0.7f)),
            Exposure      = 1.0f,
            MoonDir       = Vector3.Normalize(new Vector3(-0.2f, 0.1f, -0.7f)),
            StarIntensity = 1.0f,
            MilkyWay      = 1.0f,
            Turbidity     = 2.0f,
            AtmosphereH   = 1.0f,
        };

        public ProceduralSkyRenderable(string name)
            : base(
                name,
                VertexPositionTexture.Layout,
                CreateSkyPipelineDesc()
              )
        {
        }

        private static GraphicsPipelineDescription CreateSkyPipelineDesc()
        {
            return new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleDisabled,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: false,
                    depthWriteEnabled: false,
                    comparisonKind: ComparisonKind.Always),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.None,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.CounterClockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = Array.Empty<ResourceLayout>()
            };
        }

        public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
        {
            base.Initialize(gd, factory);

            _uboCamera = new SkyUniformBuffer<CameraUBO>($"{Name}_CameraUBO");
            _uboTime   = new SkyUniformBuffer<TimeUBO>($"{Name}_TimeUBO");
            _uboParams = new SkyUniformBuffer<SkyParamsUBO>($"{Name}_ParamsUBO");
            _uboCamera.Initialize(gd);
            _uboTime.Initialize(gd);
            _uboParams.Initialize(gd);

            _uboLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("Camera", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Time",   ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("Params", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                )
            );

            var pd = PipelineDescription;
            var layouts = new List<ResourceLayout>(pd.ResourceLayouts ?? Array.Empty<ResourceLayout>())
            {
                _uboLayout
            };
            pd.ResourceLayouts = layouts.ToArray();
            PipelineDescription = pd;

            SetShaders(_shaders);
            CreatePipeline(factory);

            var verts = new VertexPositionTexture[]
            {
                new VertexPositionTexture(new Vector2(-1f, -1f), new Vector2(0f, 0f)),
                new VertexPositionTexture(new Vector2( 1f, -1f), new Vector2(1f, 0f)),
                new VertexPositionTexture(new Vector2( 1f,  1f), new Vector2(1f, 1f)),
                new VertexPositionTexture(new Vector2(-1f,  1f), new Vector2(0f, 1f)),
            };
            var indices = new ushort[] { 0,1,2, 0,2,3 };

            SetVertexData(verts);
            _indexBuffer?.Dispose();
            _indexBuffer = gd.ResourceFactory.CreateBuffer(
                new BufferDescription((uint)(indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
            gd.UpdateBuffer(_indexBuffer, 0, indices);

            _initialized = true;
        }

        public override void Draw(CommandList cl)
        {
            if (!_initialized || _vertexBuffer == null || _pipeline == null) return;

            cl.SetPipeline(_pipeline);
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetGraphicsResourceSet(0, _uboSet); // set = 0

            cl.DrawIndexed(6, 1, 0, 0, 0);
        }

        public void Sync(Camera camera, float deltaTime, uint vpW, uint vpH, GraphicsDevice gd)
        {
            if (camera == null || gd == null) return;
            _time += deltaTime;

            var view = camera.GetViewMatrix();
            var proj = camera.GetProjectionMatrix();
            Matrix4x4.Invert(view, out var invView);
            Matrix4x4.Invert(proj, out var invProj);

            var camUBO = new CameraUBO
            {
                InvView   = invView,
                InvProj   = invProj,
                CameraPos = camera.Position
            };
            _uboCamera.Update(gd, camUBO);

            var timeUBO = new TimeUBO
            {
                Time          = _time,
                DeltaTime     = deltaTime,
                ViewportWidth = vpW,
                ViewportHeight= vpH
            };
            _uboTime.Update(gd, timeUBO);

            _uboParams.Update(gd, _params);

            if (_uboSet == null)
            {
                _uboSet = gd.ResourceFactory.CreateResourceSet(
                    new ResourceSetDescription(_uboLayout, _uboCamera.Get(), _uboTime.Get(), _uboParams.Get()));
            }
        }

        public ProceduralSkyRenderable SetSunDir(Vector3 dir)        { _params.SunDir = Vector3.Normalize(dir); return this; }
        public ProceduralSkyRenderable SetMoonDir(Vector3 dir)       { _params.MoonDir = Vector3.Normalize(dir); return this; }
        public ProceduralSkyRenderable SetExposure(float v)          { _params.Exposure = v; return this; }
        public ProceduralSkyRenderable SetStarIntensity(float v)     { _params.StarIntensity = v; return this; }
        public ProceduralSkyRenderable SetMilkyWay(float v)          { _params.MilkyWay = v; return this; }
        public ProceduralSkyRenderable SetTurbidity(float v)         { _params.Turbidity = v; return this; }
        public ProceduralSkyRenderable SetAtmosphereHeight(float v)  { _params.AtmosphereH = v; return this; }

        public override void Dispose()
        {
            base.Dispose();
            _indexBuffer?.Dispose();
            _uboSet?.Dispose();
            _uboLayout?.Dispose();
            _uboCamera?.Dispose();
            _uboTime?.Dispose();
            _uboParams?.Dispose();
        }
    }

    public sealed class ProceduralSkySystem : IDisposable
    {
        public ProceduralSkyRenderable Renderable { get; private set; }

        private RendererBase _renderer;
        private readonly string _vsPath;
        private readonly string _fsPath;
        private readonly byte[] _vsBytes;
        private readonly byte[] _fsBytes;

        private readonly Clogger LOGGER = LogManager.GetLogger("SkySystem");
        private Action<RendererBase> _prevUpdate;

        public ProceduralSkySystem(string vertexShaderPath, string fragmentShaderPath)
        {
            _vsPath = vertexShaderPath;
            _fsPath = fragmentShaderPath;
        }

        public ProceduralSkySystem(byte[] vertexShaderBytes, byte[] fragmentShaderBytes)
        {
            _vsBytes = vertexShaderBytes;
            _fsBytes = fragmentShaderBytes;
        }

        public ProceduralSkySystem Attach(RendererBase renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

            Renderable = new ProceduralSkyRenderable("ProceduralSky");

            if (_vsBytes != null && _fsBytes != null)
                Renderable.SetShaderBytes(_vsBytes, _fsBytes);
            else if (!string.IsNullOrEmpty(_vsPath) && !string.IsNullOrEmpty(_fsPath))
                Renderable.SetShaderPaths(_vsPath, _fsPath);
            else
                throw new InvalidOperationException("必须提供天空着色器的路径或字节。");

            _renderer.AddRenderable(Renderable);

            _prevUpdate = _renderer.OnUpdate;
            _renderer.OnUpdate = r =>
            {
                try
                {
                    _prevUpdate?.Invoke(r);
                }
                catch (Exception ex)
                {
                    LOGGER.Warn($"上一个 OnUpdate 回调抛出异常：{ex.Message}");
                }

                var gd   = r._graphicsDevice;
                var cam  = r.Camera;
                var dt   = r.GetDeltaTime();
                var fb   = gd?.SwapchainFramebuffer;
                uint w = fb?.Width  ?? (uint)_renderer._window.Width;
                uint h = fb?.Height ?? (uint)_renderer._window.Height;

                Renderable.Sync(cam, dt, w, h, gd);
            };

            return this;
        }

        public void Detach()
        {
            if (_renderer == null) return;

            _renderer.OnUpdate = _prevUpdate;

            try { _renderer.RemoveRenderable(Renderable?.Name); }
            catch { /* ignore */ }

            Renderable?.Dispose();
            Renderable = null;
            _renderer = null;
        }

        public void Dispose() => Detach();
    }
}
