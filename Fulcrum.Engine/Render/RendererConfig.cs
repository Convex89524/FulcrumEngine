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

using Veldrid;

namespace Fulcrum.Engine.Render
{
    // 渲染器配置类
    public class RendererConfig
    {
        public GraphicsBackend Backend { get; set; } = GraphicsBackend.Vulkan;
        public string WindowTitle { get; set; } = "Fulcrum Engine (by Convex89524)";
        public int WindowWidth { get; set; } = 800;
        public int WindowHeight { get; set; } = 600;
        public bool EnableValidation { get; set; } = true;
        public string ShaderDirectory { get; set; } = "shaders";
        public PixelFormat? DepthFormat { get; set; } = PixelFormat.D24_UNorm_S8_UInt;
        public RgbaFloat ClearColor { get; set; } = RgbaFloat.Black;
    }
}