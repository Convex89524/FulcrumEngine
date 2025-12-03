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
using Fulcrum.Engine.App;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Sound;

namespace Fulcrum.Engine;

public abstract class ScriptBase
{
    public virtual string ScriptId { get; protected set; } = "UnnamedScript";

    public abstract void OnLoad(Clogger logger, RenderApp renderApp, AudioEngine audioEngine);

    public abstract void OnUpdate(int currentTick, AudioEngine audioEngine);

    public abstract void OnRenderFrame(RendererBase rendererBase);

    public abstract void OnUninstall();
}