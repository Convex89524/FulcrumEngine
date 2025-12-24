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

// LightComponents.cs
using System;
using System.Collections.Generic;
using System.Numerics;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Render.Utils;
using Fulcrum.Engine.Scene;

namespace Fulcrum.Engine.GameObjectComponent.Light
{
    public abstract class LightComponent : Component
    {
        /// <summary>光颜色（线性空间）</summary>
        public Vector3 Color { get; set; } = new Vector3(1f, 1f, 1f);

        /// <summary>光强度（简单乘子）</summary>
        public float Intensity { get; set; } = 1.0f;

        /// <summary>当前世界位置</summary>
        public Vector3 WorldPosition =>
            Owner?.Transform?.Position ?? Vector3.Zero;

        /// <summary>当前世界前向（+Z）</summary>
        public Vector3 Forward =>
            Owner?.Transform?.Forward ?? Vector3.UnitZ;

        protected override void OnEnable()
        {
            LightManager.RegisterLight(this);
        }

        protected override void OnDisable()
        {
            LightManager.UnregisterLight(this);
        }

        protected override void OnDestroy()
        {
            LightManager.UnregisterLight(this);
        }
    }

    public class DirectionalLightComponent : LightComponent
    {
        public bool IsMainLight { get; set; } = true;
    }

    public class PointLightComponent : LightComponent
    {
        public float Range { get; set; } = 10f;
    }
    
    public class SpotLightComponent : LightComponent
    {
        /// <summary>内圆锥角度（度）</summary>
        public float InnerAngle { get; set; } = 20f;

        /// <summary>外圆锥角度（度）</summary>
        public float OuterAngle { get; set; } = 30f;

        /// <summary>影响半径。</summary>
        public float Range { get; set; } = 15f;
    }
    
    public static class LightManager
    {
        private static readonly List<MeshRenderer> _meshRenderers = new List<MeshRenderer>();
        private static readonly List<DirectionalLightComponent> _directionalLights = new List<DirectionalLightComponent>();
        private static readonly List<PointLightComponent> _pointLights = new List<PointLightComponent>();
        private static readonly List<SpotLightComponent> _spotLights = new List<SpotLightComponent>();

        // 全局环境光
        public static Vector3 AmbientColor { get; set; } = new Vector3(0.03f, 0.03f, 0.03f);

        // 全局 AO 缩放
        public static float GlobalAo { get; set; } = 1.0f;

        #region 注册 / 注销

        internal static void RegisterMeshRenderer(MeshRenderer r)
        {
            if (r == null) return;
            if (!_meshRenderers.Contains(r)) _meshRenderers.Add(r);
        }

        internal static void UnregisterMeshRenderer(MeshRenderer r)
        {
            if (r == null) return;
            _meshRenderers.Remove(r);
        }

        internal static void RegisterLight(LightComponent light)
        {
            switch (light)
            {
                case DirectionalLightComponent d:
                    if (!_directionalLights.Contains(d)) _directionalLights.Add(d);
                    break;
                case PointLightComponent p:
                    if (!_pointLights.Contains(p)) _pointLights.Add(p);
                    break;
                case SpotLightComponent s:
                    if (!_spotLights.Contains(s)) _spotLights.Add(s);
                    break;
            }
        }

        internal static void UnregisterLight(LightComponent light)
        {
            switch (light)
            {
                case DirectionalLightComponent d:
                    _directionalLights.Remove(d);
                    break;
                case PointLightComponent p:
                    _pointLights.Remove(p);
                    break;
                case SpotLightComponent s:
                    _spotLights.Remove(s);
                    break;
            }
        }

        #endregion

        #region 更新 LightingParams

        public static void UpdateLighting(RendererBase renderer)
        {
            if (renderer == null) return;

            Vector3 cameraPos = renderer.Camera != null
                ? renderer.Camera.Position
                : Vector3.Zero;

            ComputeMainLight(out Vector3 lightDir, out Vector3 lightColor);

            foreach (var mr in _meshRenderers.ToArray())
            {
                if (mr == null || mr.Renderable == null) continue;

                var lp = new GeometryRenderable3D.LightingParams
                {
                    LightDir = lightDir,
                    LightColor     = lightColor + AmbientColor,
                    CameraPos = cameraPos,
                    Roughness      = mr.Roughness,
                    Metallic       = mr.Metallic,
                    AO             = mr.Ao * GlobalAo
                };

                mr.Renderable.UpdateLighting(renderer._graphicsDevice, in lp);
            }
        }

        private static void ComputeMainLight(out Vector3 dir, out Vector3 color)
        {
            foreach (var d in _directionalLights)
            {
                if (d == null || !d.Enabled) continue;
                if (!d.IsMainLight) continue;

                dir = -Vector3.Normalize(d.Forward);
                color = d.Color * d.Intensity;
                return;
            }

            foreach (var d in _directionalLights)
            {
                if (d == null || !d.Enabled) continue;

                dir = -Vector3.Normalize(d.Forward);
                color = d.Color * d.Intensity;
                return;
            }

            foreach (var s in _spotLights)
            {
                if (s == null || !s.Enabled) continue;

                dir = -Vector3.Normalize(s.Forward);
                color = s.Color * s.Intensity;
                return;
            }

            foreach (var p in _pointLights)
            {
                if (p == null || !p.Enabled) continue;

                var toOrigin = -p.WorldPosition;
                dir = toOrigin.LengthSquared() > 1e-6f
                    ? Vector3.Normalize(toOrigin)
                    : Vector3.UnitY;

                color = p.Color * p.Intensity;
                return;
            }

            dir = Vector3.Normalize(new Vector3(0.3f, -1f, 0.2f));
            color = Vector3.Zero;
        }

        #endregion
    }
}
