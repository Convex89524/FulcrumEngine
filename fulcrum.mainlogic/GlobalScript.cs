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
using Fulcrum.Common;
using Fulcrum.Engine;
using Fulcrum.Engine.App;
using Fulcrum.Engine.GameObjectComponent;
using Fulcrum.Engine.PrefabGameObject;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Scene;
using Fulcrum.Engine.Sound;

namespace fulcrum.mainlogic;

public class GlobalScript : ScriptBase
{
    private EngineCoordinator _engineCoordinator;
    private SoundSource _music;

    public override string ScriptId { get; protected set; } = "GlobalScript";
    
    public override void OnLoad(Clogger logger, RenderApp renderApp, AudioEngine audioEngine)
    {
        var sfxBus = audioEngine.GetBus("SFX");

        var dopplerEffect = new GlobalDopplerEffect(
            sfxBus, 
            () => audioEngine.Listener
        )
        {
            DopplerScale = 1.0f,
            SpeedOfSound = 343f
        };
        sfxBus.AddEffect(dopplerEffect);
        
        // renderApp.AddModule(new BlackHoleModule());
        
        var scene = Scene.CurrentScene;
        if (scene == null)
        {   
            scene = new Scene();
            Scene.SetCurrent(scene);
        }
        
        var rootObj = new GameObject("Root");
        scene.AddRoot(rootObj);
        
        // 摄像机
        var cameraGo = scene.Instantiate("MainCamera");
        cameraGo.Transform.Position = new Vector3(5, 3, 8);
        cameraGo.Transform.LookAt(Vector3.Zero, Vector3.UnitY);
        var camComp = cameraGo.AddComponent<CameraComponent>();
        camComp.IsMainCamera = true;
        
        // 音乐
        var musicObj = new GameObject("Music");
        rootObj.AddChild(musicObj);
        var moMusicCop = musicObj.AddComponent<SoundSourceComponent>();

        moMusicCop.BusName = SoundBuses.SFX_BUS;
        moMusicCop.SoundPath = "music.wav";
        moMusicCop.Loop = true;
        
        moMusicCop.CreateRuntimeSource();
        moMusicCop.RuntimeSource?.Play();


        // 球
        var sphere = new Sphere("Sphere1", radius: 0.5f);
        scene.AddRoot(sphere);

        // 圆柱
        var cyl = new Cylinder("Cylinder1", radius: 0.5f, height: 2.0f);
        scene.AddRoot(cyl);

        // 三角柱
        var tri = new TriangularPrism("TriPrism1", width: 1.0f, height: 1.5f, depth: 1.0f);
        scene.AddRoot(tri);

        // 楔体
        var wedge = new Wedge("Wedge1", new Vector3(1, 1, 2));
        scene.AddRoot(wedge);

        // 按 X 轴排成一排（只是几何体）
        var primitives = new GameObject[]
        {
            sphere,
            cyl,
            tri,
            wedge
        };

        float startX = -3.0f;   // 起点 X
        float spacing = 2.0f;   // 间距
        float y = 0.0f;         // 统一放在地平面
        float z = 0.0f;         // 一条直线

        for (int i = 0; i < primitives.Length; i++)
        {
            var go = primitives[i];
            if (go == null) continue;

            go.Transform.Position = new Vector3(
                startX + i * spacing,
                y,
                z
            );
        }
    }

    public override void OnUpdate(int currentTick, AudioEngine audioEngine)
    {
    }

    public override void OnRenderFrame(RendererBase rendererBase)
    {
    }

    public override void OnUninstall()
    {
        _engineCoordinator?.Dispose();
        _music?.Dispose();
    }
}
