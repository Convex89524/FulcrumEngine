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

namespace Fulcrum.Engine.Scene
{
    public class GameObject
    {
        public string Name { get; set; }

        // 变换
        public Transform Transform { get; } = new Transform();

        // 组件容器
        public List<Component> Components { get; } = new List<Component>();

        // 子物体
        public List<GameObject> Children { get; } = new List<GameObject>();

        // 父物体
        public GameObject? Parent { get; private set; }

        // 场景引用
        public Scene _scene;

        // 标签 / 层
        public string Tag { get; set; } = "Untagged";
        public int Layer { get; set; } = 0;

        // 激活体系
        public bool ActiveSelf { get; private set; } = true;
        public bool ActiveInHierarchy
        {
            get
            {
                if (!ActiveSelf) return false;
                var p = Parent;
                while (p != null)
                {
                    if (!p.ActiveSelf) return false;
                    p = p.Parent;
                }
                return true;
            }
        }

        public GameObject(string name = "GameObject")
        {
            Name = name;
            Transform.Owner = this;
        }

        /// <summary>
        /// 设置自身激活状态（会级联触发组件 OnEnable/OnDisable/Start）
        /// </summary>
        public void SetActive(bool active)
        {
            if (ActiveSelf == active) return;
            ActiveSelf = active;

            _ReEvalEnableRecursive();
        }

        /// <summary>
        /// 添加子对象并设置父对象引用
        /// </summary>
        public void AddChild(GameObject child)
        {
            if (child == null || child == this) return;
            if (child.Parent == this) return;

            child.Detach();
            Children.Add(child);
            child.Parent = this;

            if (child.Transform.Owner != child)
                child.Transform.Owner = child;

            child._SetSceneRecursive(_scene);

            child._EnsureAwakeRecursive();
            if (child.ActiveInHierarchy) child._EnsureEnabledStartRecursive();
            else child._EnsureDisabledRecursive();
        }

        /// <summary>从父对象剥离（成为无父节点）</summary>
        public void Detach()
        {
            if (Parent == null) return;
            Parent.Children.Remove(this);
            Parent = null;

            _scene?.AddRoot(this);
        }

        /// <summary>移除一个子对象（不销毁）</summary>
        public void RemoveChild(GameObject child)
        {
            if (child == null || child.Parent != this) return;
            Children.Remove(child);
            child.Parent = null;
            _scene?.AddRoot(child);
        }

        /// <summary>根据子对象名称获取子对象（递归，原功能保留）</summary>
        public GameObject? GetChildByName(string name)
        {
            foreach (var child in Children)
            {
                if (child.Name == name)
                    return child;

                var found = child.GetChildByName(name);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>获取父对象（原功能保留）</summary>
        public GameObject? GetParent() => Parent;


        public T AddComponent<T>() where T : Component, new()
        {
            var comp = new T();
            _AttachComponent(comp);
            return comp;
        }

        public Component AddComponent(Type type)
        {
            if (!typeof(Component).IsAssignableFrom(type))
                throw new ArgumentException("Type must inherit Component", nameof(type));
            var comp = (Component)Activator.CreateInstance(type);
            _AttachComponent(comp);
            return comp;
        }

        public bool RemoveComponent(Component comp)
        {
            if (comp == null || comp.Owner != this) return false;
            if (!Components.Remove(comp)) return false;

            comp.Enabled = false;
            comp._OnDisableIfNeeded();
            comp._OnDestroy();
            comp.Owner = null;
            return true;
        }

        public T GetComponent<T>() where T : Component
            => Components.OfType<T>().FirstOrDefault();

        public Component GetComponent(Type t)
            => Components.FirstOrDefault(c => t.IsInstanceOfType(c));

        public T GetComponentInChildren<T>() where T : Component
        {
            var c = GetComponent<T>();
            if (c != null) return c;
            foreach (var child in Children)
            {
                var cc = child.GetComponentInChildren<T>();
                if (cc != null) return cc;
            }
            return null;
        }

        public void GetComponentsInChildren<T>(List<T> results) where T : Component
        {
            var local = Components.OfType<T>();
            results.AddRange(local);
            foreach (var child in Children)
                child.GetComponentsInChildren(results);
        }

        private void _AttachComponent(Component comp)
        {
            if (comp == null) return;
            if (comp.Owner == this) return;

            comp.Owner = this;
            Components.Add(comp);

            comp._OnAwake();
            if (ActiveInHierarchy && comp.Enabled)
            {
                comp._OnEnableIfNeeded();
                comp._OnStartIfNeeded();
            }
        }

        /// <summary>给本对象所有组件发消息（反射调用无参或单参方法）</summary>
        public void SendMessage(string method, object arg = null, bool includeInactive = false)
        {
            if (!includeInactive && !ActiveInHierarchy) return;

            foreach (var c in Components.ToArray())
            {
                var mi = c.GetType().GetMethod(method,
                    arg == null ? Type.EmptyTypes : new[] { arg.GetType() });
                if (mi != null)
                {
                    mi.Invoke(c, arg == null ? Array.Empty<object>() : new[] { arg });
                }
            }
        }

        /// <summary>向自身与全子树广播消息</summary>
        public void BroadcastMessage(string method, object arg = null, bool includeInactive = false)
        {
            if (!includeInactive && !ActiveInHierarchy) return;

            SendMessage(method, arg, includeInactive);
            foreach (var child in Children)
                child.BroadcastMessage(method, arg, includeInactive);
        }

        /// <summary>深度优先遍历（含自身）</summary>
        public IEnumerable<GameObject> Traverse()
        {
            yield return this;
            foreach (var c in Children)
                foreach (var n in c.Traverse())
                    yield return n;
        }

        internal void _EnsureAwakeRecursive()
        {
            foreach (var c in Components) c._OnAwake();
            foreach (var ch in Children) ch._EnsureAwakeRecursive();
        }

        internal void _EnsureEnabledStartRecursive()
        {
            foreach (var c in Components)
            {
                c._OnEnableIfNeeded();
                c._OnStartIfNeeded();
            }
            foreach (var ch in Children) ch._EnsureEnabledStartRecursive();
        }

        internal void _EnsureDisabledRecursive()
        {
            foreach (var c in Components) c._OnDisableIfNeeded();
            foreach (var ch in Children) ch._EnsureDisabledRecursive();
        }

        internal void _UpdateRecursive(double dt)
        {
            if (!ActiveInHierarchy) return;
            foreach (var c in Components) c._OnUpdate(dt);
            foreach (var ch in Children) ch._UpdateRecursive(dt);
        }

        internal void _FixedUpdateRecursive()
        {
            if (!ActiveInHierarchy) return;
            foreach (var c in Components) c._OnFixedUpdate();
            foreach (var ch in Children) ch._FixedUpdateRecursive();
        }

        internal void _LateUpdateRecursive()
        {
            if (!ActiveInHierarchy) return;
            foreach (var c in Components) c._OnLateUpdate();
            foreach (var ch in Children) ch._LateUpdateRecursive();
        }

        internal void _DestroyRecursive()
        {
            foreach (var ch in Children.ToArray())
                ch._DestroyRecursive();

            foreach (var c in Components.ToArray())
            {
                c.Enabled = false;
                c._OnDisableIfNeeded();
                c._OnDestroy();
                c.Owner = null;
            }
            Components.Clear();

            if (Parent != null) Parent.Children.Remove(this);
            else _scene?.RootObjects.Remove(this);

            Parent = null;
            _scene = null;
        }

        internal void _SetSceneRecursive(Scene scene)
        {
            _scene = scene;
            foreach (var ch in Children)
                ch._SetSceneRecursive(scene);
        }

        private void _ReEvalEnableRecursive()
        {
            foreach (var c in Components)
            {
                if (ActiveInHierarchy && c.Enabled)
                {
                    c._OnEnableIfNeeded();
                    c._OnStartIfNeeded();
                }
                else
                {
                    c._OnDisableIfNeeded();
                }
            }
            foreach (var ch in Children)
                ch._ReEvalEnableRecursive();
        }
    }
}