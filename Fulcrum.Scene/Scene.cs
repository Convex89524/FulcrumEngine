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

using CMLS.CLogger;

namespace Fulcrum.Engine.Scene
{
    public class Scene
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("SceneSystem");
        
        /// <summary>当前激活场景</summary>
        public static Scene CurrentScene { get; private set; }

        /// <summary>场景内的根对象（无父对象）</summary>
        public List<GameObject> RootObjects { get; } = new List<GameObject>();

        /// <summary>待销毁队列：延迟至帧尾统一处理</summary>
        private readonly List<GameObject> _destroyQueue = new();

        /// <summary>固定步长</summary>
        public double FixedDeltaTime { get; set; } = 1.0 / 50.0;
        private double _fixedAccumulator = 0.0;
        
        
        public static event EventHandler<Scene> OnBinderSceneChanged;

        /// <summary>切换当前场景</summary>
        public static void SetCurrent(Scene scene)
        {
            if (CurrentScene == scene) return;
            LOGGER.Info($"切换当前场景 => {scene?.GetType().Name ?? "null"}");
            CurrentScene = scene;
            
            OnBinderSceneChanged?.Invoke(null, scene);
        }

        /// <summary>将 GameObject 作为根加入场景（不会更改其子树）</summary>
        public void AddRoot(GameObject go)
        {
            if (go == null) return;
            if (go.Parent != null) go.Detach();
            if (!RootObjects.Contains(go))
            {
                RootObjects.Add(go);
                LOGGER.Debug($"添加根对象: {go.Name}");
                go._scene = this;
                go._EnsureAwakeRecursive();
                if (go.ActiveInHierarchy) go._EnsureEnabledStartRecursive();
            }
        }

        /// <summary>从场景移除根对象（不销毁对象，可手动放到其他场景）</summary>
        public void RemoveRoot(GameObject go)
        {
            if (go == null) return;
            RootObjects.Remove(go);
            LOGGER.Debug($"移除根对象: {go.Name}");
            go._scene = null;
        }

        /// <summary>工厂：实例化一个空 GameObject（名、激活状态可指定）</summary>
        public GameObject Instantiate(string name = "GameObject", bool active = true)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            AddRoot(go);
            return go;
        }

        /// <summary>标记销毁（帧尾执行）</summary>
        public void Destroy(GameObject go)
        {
            if (go == null) return;
            if (!_destroyQueue.Contains(go)) _destroyQueue.Add(go);
        }

        /// <summary>场景内查找（按名，递归）</summary>
        public GameObject Find(string name)
        {
            foreach (var root in RootObjects)
            {
                if (root.Name == name) return root;
                var f = root.GetChildByName(name);
                if (f != null) return f;
            }
            return null;
        }

        /// <summary>场景内按组件类型查找（第一个）</summary>
        public T FindObjectOfType<T>() where T : Component
            => RootObjects.Select(r => r.GetComponentInChildren<T>()).FirstOrDefault(c => c != null);

        /// <summary>场景内按组件类型查找（全部）</summary>
        public List<T> FindObjectsOfType<T>() where T : Component
        {
            var list = new List<T>();
            foreach (var r in RootObjects)
                r.GetComponentsInChildren(list);
            return list;
        }

        private void _ForEachAlive(Action<GameObject> action)
        {
            foreach (var r in RootObjects.ToArray())
                _Walk(r, action);
        }

        private static void _Walk(GameObject go, Action<GameObject> action)
        {
            action(go);
            foreach (var c in go.Children.ToArray())
                _Walk(c, action);
        }

        public void OnRenderFrame(double dt)
        {
            foreach (var r in RootObjects.ToArray())
                r._UpdateRecursive(dt);
        }
        
        public void Step(double deltaTime)
        {
            if (deltaTime < 0) deltaTime = 0;
            if (deltaTime > 0.25) deltaTime = 0.25;
            
            _fixedAccumulator += deltaTime;
            while (_fixedAccumulator + 1e-12 >= FixedDeltaTime)
            {
                foreach (var r in RootObjects.ToArray())
                    r._FixedUpdateRecursive();
                _fixedAccumulator -= FixedDeltaTime;
            }
            
            foreach (var r in RootObjects.ToArray())
                r._LateUpdateRecursive();

            if (_destroyQueue.Count > 0)
            {
                foreach (var go in _destroyQueue.ToArray())
                    go?._DestroyRecursive();
                _destroyQueue.Clear();
            }
        }
    }
}