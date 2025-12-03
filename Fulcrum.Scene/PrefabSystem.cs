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
using System.IO;
using System.Reflection;
using System.Numerics;

namespace Fulcrum.Engine.Scene
{
    public class PrefabAsset
    {
        public byte[] RawData;

        public PrefabAsset(byte[] raw)
        {
            RawData = raw;
        }

        public void Save(string path)
        {
            File.WriteAllBytes(path, RawData);
        }

        public static PrefabAsset Load(string path)
        {
            return new PrefabAsset(File.ReadAllBytes(path));
        }
    }

    public static class PrefabSystem
    {
        public static PrefabAsset CreatePrefab(GameObject root)
        {
            string temp = Path.GetTempFileName();
            Scene tempScene = new Scene();
            tempScene.AddRoot(CloneObjectTree(root));
            SceneSerializer.SaveToFile(tempScene, temp);

            var data = File.ReadAllBytes(temp);
            File.Delete(temp);
            return new PrefabAsset(data);
        }

        public static GameObject InstantiatePrefab(PrefabAsset prefab, Scene targetScene)
        {
            string temp = Path.GetTempFileName();
            File.WriteAllBytes(temp, prefab.RawData);

            Scene loadedScene = SceneSerializer.LoadFromFile(temp);
            File.Delete(temp);

            // Prefab 顶层只会有一个 root
            if (loadedScene.RootObjects.Count == 0)
                throw new InvalidDataException("Empty prefab");

            var srcRoot = loadedScene.RootObjects[0];
            var newRoot = CloneObjectTree(srcRoot);

            targetScene.AddRoot(newRoot);
            return newRoot;
        }
        
        private static GameObject CloneObjectTree(GameObject src)
        {
            var copy = new GameObject(src.Name)
            {
                Tag = src.Tag,
                Layer = src.Layer
            };
            copy.SetActive(src.ActiveSelf);

            var trs = src.Transform.GetLocalTRS();
            copy.Transform.SetLocalTRS(trs.pos, trs.rot, trs.scl);

            foreach (var comp in src.Components)
                CloneComponent(copy, comp);

            foreach (var child in src.Children)
            {
                var newChild = CloneObjectTree(child);
                copy.AddChild(newChild);
            }

            return copy;
        }

        private static void CloneComponent(GameObject dst, Component srcComp)
        {
            Type t = srcComp.GetType();
            var newComp = dst.AddComponent(t);

            if (srcComp is ISceneSerializableComponent serializableSrc &&
                newComp is ISceneSerializableComponent serializableDst)
            {
                using var ms = new MemoryStream();
                using var bw = new BinaryWriter(ms);
                serializableSrc.Serialize(bw);

                var bytes = ms.ToArray();

                using var ms2 = new MemoryStream(bytes);
                using var br = new BinaryReader(ms2);
                serializableDst.Deserialize(br);
            }
        }
    }
}
