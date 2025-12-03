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

namespace Fulcrum.Engine.Scene
{
    /// <summary>
    /// 组件基类：定义统一生命周期与启停控制。
    /// </summary>
    public abstract class Component
    {
        protected static readonly Clogger LOGGER = LogManager.GetLogger("SceneEngine");
            
        public GameObject Owner { get; internal set; }

        /// <summary>组件启用状态（受 GameObject 激活影响）</summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                _ReEvalEnableState();
            }
        }

        /// <summary>是否已执行 Start </summary>
        public bool Started { get; private set; }

        private bool _enabled = true;
        private bool _effectiveEnabled = false;

        internal void _OnAwake()
        {
            if (!_awoken)
            {
                _awoken = true;
                try { Awake(); } catch (Exception e) { _OnException("Awake", e); }
            }
        }

        internal void _OnEnableIfNeeded()
        {
            var should = _enabled && Owner.ActiveInHierarchy;
            if (should && !_effectiveEnabled)
            {
                _effectiveEnabled = true;
                try { OnEnable(); } catch (Exception e) { _OnException("OnEnable", e); }
            }
        }

        internal void _OnStartIfNeeded()
        {
            if (_effectiveEnabled && !Started)
            {
                Started = true;
                try { Start(); } catch (Exception e) { _OnException("Start", e); }
            }
        }

        internal void _OnDisableIfNeeded()
        {
            var should = _enabled && Owner.ActiveInHierarchy;
            if (!should && _effectiveEnabled)
            {
                _effectiveEnabled = false;
                try { OnDisable(); } catch (Exception e) { _OnException("OnDisable", e); }
            }
        }

        internal void _OnUpdate(double dt)
        {
            if (_effectiveEnabled)
            {
                try { Update(dt); } catch (Exception e) { _OnException("Update", e); }
            }
        }

        internal void _OnFixedUpdate()
        {
            if (_effectiveEnabled)
            {
                try { FixedUpdate(); } catch (Exception e) { _OnException("FixedUpdate", e); }
            }
        }

        internal void _OnLateUpdate()
        {
            if (_effectiveEnabled)
            {
                try { LateUpdate(); } catch (Exception e) { _OnException("LateUpdate", e); }
            }
        }

        internal void _OnDestroy()
        {
            try { OnDestroy(); } catch (Exception e) { _OnException("OnDestroy", e); }
        }

        private bool _awoken = false;

        private void _ReEvalEnableState()
        {
            if (Owner == null) return;
            if (_enabled && Owner.ActiveInHierarchy)
            {
                if (!_effectiveEnabled) _OnEnableIfNeeded();
                if (!_effectiveEnabled) return;
                if (!Started) _OnStartIfNeeded();
            }
            else
            {
                _OnDisableIfNeeded();
            }
        }

        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void Start() { }
        protected virtual void Update(double dt) { }
        protected virtual void FixedUpdate() { }
        protected virtual void LateUpdate() { }
        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }

        protected void SendMessage(string method, object arg = null, bool includeInactive = false)
            => Owner?.SendMessage(method, arg, includeInactive);

        protected void BroadcastMessage(string method, object arg = null, bool includeInactive = false)
            => Owner?.BroadcastMessage(method, arg, includeInactive);

        private static void _OnException(string phase, Exception e)
        {
            LOGGER.Error($"{phase} Exception: {e}");
        }
    }
}