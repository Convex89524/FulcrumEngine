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

using System.Numerics;
using CMLS.CLogger;
using Fulcrum.Engine;
using Fulcrum.Engine.App;
using Fulcrum.Engine.Render;
using Fulcrum.Engine.Sound;
using ImGuiNET;

namespace fulcrum.mainlogic;

public class Crosshair : ScriptBase
{
    private void DrawCrosshair(RendererBase renderer)
    {
        var vp = ImGui.GetMainViewport();
        float cx = vp.WorkPos.X + vp.WorkSize.X * 0.5f;
        float cy = vp.WorkPos.Y + vp.WorkSize.Y * 0.5f;

        var drawList = ImGui.GetForegroundDrawList(vp);

        float size = 8.0f;
        float gap = 4.0f;
        float thickness = 1.5f;
        uint color = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.9f));

        // 左
        drawList.AddLine(
            new Vector2(cx - gap - size, cy),
            new Vector2(cx - gap, cy),
            color, thickness);
        // 右
        drawList.AddLine(
            new Vector2(cx + gap, cy),
            new Vector2(cx + gap + size, cy),
            color, thickness);
        // 上
        drawList.AddLine(
            new Vector2(cx, cy - gap - size),
            new Vector2(cx, cy - gap),
            color, thickness);
        // 下
        drawList.AddLine(
            new Vector2(cx, cy + gap),
            new Vector2(cx, cy + gap + size),
            color, thickness);
    }
    public override void OnLoad(Clogger logger, RenderApp renderApp, AudioEngine audioEngine)
    {
        renderApp.Renderer.OnImGuiRender += DrawCrosshair;
    }

    public override void OnUpdate(int currentTick, AudioEngine audioEngine)
    {;
    }

    public override void OnRenderFrame(RendererBase rendererBase)
    {
    }

    public override void OnUninstall()
    {
    }
}