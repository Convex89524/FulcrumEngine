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

using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using CMLS.CLogger;

namespace Fulcrum.Engine.Scene
{
    public static class SceneSerializer
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("SceneSerializer");

        private const uint MAGIC = 0x4E435346;
        private const int VERSION = 1;

        #region Public API

        public static void SaveToFile(Scene scene, string filePath)
        {
            if (scene == null)
            {
                LOGGER.Warn("SaveToFile: scene is null, abort.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? ".");
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var bw = new BinaryWriter(fs);
            WriteScene(scene, bw);
            LOGGER.Info($"Scene saved to: {filePath}");
        }

        public static Scene LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Scene file not found.", filePath);

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);
            var scene = ReadScene(br);
            LOGGER.Info($"Scene loaded from: {filePath}");
            return scene;
        }

        #endregion

        #region Internal serialization format

        private class GameObjectRecord
        {
            public int Id;
            public int ParentId;
            public string Name;
            public string Tag;
            public int Layer;
            public bool ActiveSelf;

            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;

            public List<ComponentRecord> Components = new();
        }

        private class ComponentRecord
        {
            public string TypeName;
            public int DataLength;
            public byte[]? Data;
        }

        private static void WriteScene(Scene scene, BinaryWriter bw)
        {
            bw.Write(MAGIC);
            bw.Write(VERSION);

            var allObjects = new List<GameObject>();
            var idMap = new Dictionary<GameObject, int>();

            foreach (var root in scene.RootObjects)
            {
                foreach (var go in root.Traverse())
                {
                    idMap[go] = allObjects.Count;
                    allObjects.Add(go);
                }
            }

            bw.Write(allObjects.Count);

            foreach (var go in allObjects)
            {
                int id = idMap[go];
                int parentId = -1;

                if (go.Parent != null && idMap.TryGetValue(go.Parent, out var pid))
                    parentId = pid;

                bw.Write(id);
                bw.Write(parentId);

                bw.Write(go.Name ?? string.Empty);
                bw.Write(go.Tag ?? string.Empty);
                bw.Write(go.Layer);
                bw.Write(go.ActiveSelf);

                var trs = go.Transform.GetLocalTRS();
                WriteVector3(bw, trs.pos);
                WriteQuaternion(bw, trs.rot);
                WriteVector3(bw, trs.scl);

                bw.Write(go.Components.Count);
                foreach (var comp in go.Components)
                {
                    Type type = comp.GetType();
                    string typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
                    bw.Write(typeName);

                    if (comp is ISceneSerializableComponent serializable)
                    {
                        using var ms = new MemoryStream();
                        using (var compWriter = new BinaryWriter(ms))
                        {
                            serializable.Serialize(compWriter);
                        }

                        var bytes = ms.ToArray();
                        bw.Write(bytes.Length);
                        bw.Write(bytes);
                    }
                    else
                    {
                        bw.Write(0);
                    }
                }
            }
        }

        private static Scene ReadScene(BinaryReader br)
        {
            uint magic = br.ReadUInt32();
            if (magic != MAGIC)
                throw new InvalidDataException("Invalid scene file magic.");

            int version = br.ReadInt32();
            if (version != VERSION)
            {
                LOGGER.Warn($"Scene file version mismatch: file={version}, engine={VERSION}, try to continue.");
            }

            int count = br.ReadInt32();
            if (count < 0) throw new InvalidDataException("Invalid GameObject count.");

            var records = new GameObjectRecord[count];
            for (int i = 0; i < count; i++)
            {
                var rec = new GameObjectRecord();
                rec.Id = br.ReadInt32();
                rec.ParentId = br.ReadInt32();
                rec.Name = br.ReadString();
                rec.Tag = br.ReadString();
                rec.Layer = br.ReadInt32();
                rec.ActiveSelf = br.ReadBoolean();

                rec.LocalPosition = ReadVector3(br);
                rec.LocalRotation = ReadQuaternion(br);
                rec.LocalScale = ReadVector3(br);

                int compCount = br.ReadInt32();
                for (int c = 0; c < compCount; c++)
                {
                    var cr = new ComponentRecord();
                    cr.TypeName = br.ReadString();
                    cr.DataLength = br.ReadInt32();
                    if (cr.DataLength > 0)
                    {
                        cr.Data = br.ReadBytes(cr.DataLength);
                    }
                    records[i].Components.Add(cr);
                }

                records[i] = rec;
            }

            var scene = new Scene();
            var gos = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                var rec = records[i];
                var go = new GameObject(rec.Name);
                go.Tag = rec.Tag;
                go.Layer = rec.Layer;
                go.SetActive(rec.ActiveSelf);

                go.Transform.SetLocalTRS(rec.LocalPosition, rec.LocalRotation, rec.LocalScale);
                gos[i] = go;
            }

            for (int i = 0; i < count; i++)
            {
                var rec = records[i];
                var go = gos[i];

                if (rec.ParentId < 0)
                {
                    scene.AddRoot(go);
                }
            }

            for (int i = 0; i < count; i++)
            {
                var rec = records[i];
                var go = gos[i];

                if (rec.ParentId >= 0 && rec.ParentId < count)
                {
                    var parent = gos[rec.ParentId];
                    parent.AddChild(go);
                }
            }

            for (int i = 0; i < count; i++)
            {
                var rec = records[i];
                var go = gos[i];

                foreach (var cr in rec.Components)
                {
                    Type? t = Type.GetType(cr.TypeName);

                    if (t == null)
                    {
                        LOGGER.Warn($"Cannot resolve component type: {cr.TypeName}, skip.");
                        continue;
                    }

                    if (!typeof(Component).IsAssignableFrom(t))
                    {
                        LOGGER.Warn($"Type is not a Component: {cr.TypeName}, skip.");
                        continue;
                    }

                    Component comp;
                    try
                    {
                        comp = go.AddComponent(t);
                    }
                    catch (Exception e)
                    {
                        LOGGER.Warn($"Failed to create component {cr.TypeName}: {e.Message}");
                        continue;
                    }

                    if (cr.DataLength > 0 && cr.Data != null && comp is ISceneSerializableComponent serializable)
                    {
                        try
                        {
                            using var ms = new MemoryStream(cr.Data);
                            using var compReader = new BinaryReader(ms);
                            serializable.Deserialize(compReader);
                        }
                        catch (Exception e)
                        {
                            LOGGER.Warn($"Failed to deserialize component data for {cr.TypeName}: {e.Message}");
                        }
                    }
                }
            }

            return scene;
        }

        #endregion

        #region Helpers

        private static void WriteVector3(BinaryWriter bw, Vector3 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
        }

        private static Vector3 ReadVector3(BinaryReader br)
        {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float z = br.ReadSingle();
            return new Vector3(x, y, z);
        }

        private static void WriteQuaternion(BinaryWriter bw, Quaternion q)
        {
            bw.Write(q.X);
            bw.Write(q.Y);
            bw.Write(q.Z);
            bw.Write(q.W);
        }

        private static Quaternion ReadQuaternion(BinaryReader br)
        {
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float z = br.ReadSingle();
            float w = br.ReadSingle();
            return new Quaternion(x, y, z, w);
        }

        #endregion
    }
    
    public interface ISceneSerializableComponent
    {
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }
}
