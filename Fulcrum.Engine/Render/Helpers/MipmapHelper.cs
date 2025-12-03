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

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Fulcrum.Engine.Render.Helpers;

internal static class MipmapHelper
{
    public static int ComputeMipLevels(int width, int height)
    {
        return 1 + (int) Math.Floor(Math.Log((double) Math.Max(width, height), 2.0));
    }

    public static int GetDimension(int largestLevelDimension, int mipLevel)
    {
        int val2 = largestLevelDimension;
        for (int index = 0; index < mipLevel; ++index)
            val2 /= 2;
        return Math.Max(1, val2);
    }

    internal static Image<Rgba32>[] GenerateMipmaps(Image<Rgba32> baseImage)
    {
        Image<Rgba32>[] mipmaps = new Image<Rgba32>[MipmapHelper.ComputeMipLevels(baseImage.Width, baseImage.Height)];
        mipmaps[0] = baseImage;
        int index1 = 1;
        int num = baseImage.Width;
        int newHeight;
        for (int index2 = baseImage.Height; num != 1 || index2 != 1; index2 = newHeight)
        {
            int newWidth = Math.Max(1, num / 2);
            newHeight = Math.Max(1, index2 / 2);
            Image<Rgba32> image = baseImage.Clone<Rgba32>((Action<IImageProcessingContext>) (context => context.Resize(newWidth, newHeight, KnownResamplers.Lanczos3)));
            mipmaps[index1] = image;
            ++index1;
            num = newWidth;
        }
        return mipmaps;
    }
    public static int CalculateMipLevels(int width, int height)
    {
        int levels = 1;
        while ((width | height) >> levels != 0)
            levels++;

        return levels;
    }
}