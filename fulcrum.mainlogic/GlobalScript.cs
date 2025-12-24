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
using Fulcrum.Engine.GameObjectComponent.Light;
using Fulcrum.Engine.GameObjectComponent.Phys;
using Fulcrum.Engine.PrefabGameObject;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Render.Modules;
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
        
        renderApp.AddModule(new BlackHoleModule());
        
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
    }

    public override void OnUpdate(int currentTick, AudioEngine audioEngine)
    {
    }

    public override void OnRenderFrame(RendererBase rendererBase)
    {
    }

    public override void OnUninstall()
    {
        FulcrumEngine.SaveCurrentScene("fulcrum_scene");
        
        _engineCoordinator?.Dispose();
        _music?.Dispose();
    }
}
