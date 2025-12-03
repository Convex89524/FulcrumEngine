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
using System.IO;
using System.Reflection;
using Veldrid;
using Veldrid.ImageSharp;

namespace Fulcrum.DefaultResources
{
    public static class ResourceLoader
    {
        public static ImageSharpTexture LoadFromResx(string name)
        {
            var bitmap = (System.Drawing.Bitmap)Resources1.ResourceManager.GetObject(name);
            if (bitmap == null)
                throw new FileNotFoundException($"找不到资源 {name} 于 Resources1.resx");

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                return new ImageSharpTexture(ms, false);
            }
        }
    }
}