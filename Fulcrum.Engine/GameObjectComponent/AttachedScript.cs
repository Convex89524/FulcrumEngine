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
using CMLS.CLogger;
using Fulcrum.Engine.Scene;

namespace Fulcrum.Engine.GameObjectComponent
{
    public class AttachedScript : Component
    {
        /// <summary>脚本标识</summary>
        public virtual string ScriptId { get; protected set; } = "UnnamedAttachedScript";

        /// <summary>日志</summary>
        public virtual Clogger LOGGER { get; set; }

        /// <summary>便捷访问</summary>
        protected GameObject gameObject => Owner;
        protected Transform transform   => Owner?.Transform; 
        protected Scene.Scene scene     => Owner?._scene;

        public AttachedScript()
        {
            LOGGER = LogManager.GetLogger("sct:" + ScriptId);
        }

        protected void RefreshLoggerNameIfOwnerReady()
        {
            if (Owner != null)
            {
                LOGGER = LogManager.GetLogger(Owner.Name + ":sct:" + ScriptId);
            }
        }

        protected void PrepareAfterAttached()
        {
            RefreshLoggerNameIfOwnerReady();
        }
    }
}