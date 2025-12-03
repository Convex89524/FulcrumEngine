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

using Fulcrum.Engine.App;
using Fulcrum.Engine.App.Render;
using Fulcrum.Engine.Render;

public sealed class SkyModule : RenderApp.IModule
{
    private ProceduralSkySystem skySystem;
    
    public void OnLoad(RenderApp app, RendererBase renderer)
    {
        skySystem = new ProceduralSkySystem(
             @"shaders\sky.vert",
             @"shaders\sky.frag"
        );
        skySystem.Attach(renderer);
    }

    public void OnUpdate(RenderApp app, RendererBase renderer, float deltaTime)
    {
        
    }

    public void Dispose()
    {
    }
}