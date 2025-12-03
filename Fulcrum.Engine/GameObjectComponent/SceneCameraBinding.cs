// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License
// as published by the Free Software Foundation, version 3 (GPLv3 only).
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Numerics;
using CMLS.CLogger;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Scene;

namespace Fulcrum.Engine.GameObjectComponent
{
    public class SceneCameraBinding : Component
    {
        public bool UseTransformForward = true;

        public Vector3 LocalForward = new Vector3(0, 0, -1);

        public bool SyncUpVector = true;

        private Clogger _logger;

        public SceneCameraBinding()
        {
            _logger = LogManager.GetLogger("SceneCameraBinding");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (FulcrumEngine.RenderApp?.Renderer?.Camera == null)
            {
                _logger.Warn("SceneCameraBinding enabled, but Renderer or Camera is null. Binding will be skipped until they are available.");
            }

            if (Owner == null)
            {
                _logger.Warn("SceneCameraBinding enabled, but Owner is null.");
            }
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            var renderer = FulcrumEngine.RenderApp?.Renderer;
            if (renderer == null)
                return;

            var camera = renderer.Camera;
            if (camera == null)
                return;

            if (Owner == null)
                return;

            var t = Owner.Transform;
            if (t == null)
                return;

            var pos = t.Position;

            Vector3 forwardWorld;
            if (UseTransformForward)
            {
                forwardWorld = t.Forward;
            }
            else
            {
                var m = t.LocalToWorldMatrix;
                var dir = Vector3.TransformNormal(LocalForward, m);
                forwardWorld = Vector3.Normalize(dir);
            }

            if (forwardWorld.LengthSquared() < 1e-6f)
            {
                forwardWorld = new Vector3(0, 0, -1);
            }

            Vector3 upWorld = camera.Up;
            if (SyncUpVector)
            {
                upWorld = t.Up;
                if (upWorld.LengthSquared() < 1e-6f)
                    upWorld = Vector3.UnitY;
            }

            camera.Position = pos;
            camera.Target   = pos + forwardWorld;
            camera.Up       = upWorld;
        }
    }
}
