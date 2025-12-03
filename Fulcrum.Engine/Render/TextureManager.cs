﻿// Copyright (C) 2025-2029 Convex89524
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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using CMLS.CLogger;
using Fulcrum.Common;
using SharpEXR;
using Veldrid;
using Veldrid.ImageSharp;

namespace Fulcrum.Engine.Render
{
    public class CubeMapTextureResource : RenderResource
    {
        private Texture _texture;
        private TextureView _textureView;
        private readonly string[] _filePaths;

        public CubeMapTextureResource(string name, params string[] faces)
        {
            if (faces == null || faces.Length != 6)
                throw new ArgumentException("CubeMap 必须提供 6 张贴图（posX, negX, posY, negY, posZ, negZ）。", nameof(faces));

            Name = name;
            _filePaths = faces.ToArray();
        }

        public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
        {
            try
            {
                string texturesRoot = Path.Combine(Global.ResourcesPath, "textures");
                string[] fullPaths = _filePaths
                    .Select(f => Path.Combine(texturesRoot, f))
                    .ToArray();

                var cubeImage = new ImageSharpCubemapTexture(
                    fullPaths[0],
                    fullPaths[1],
                    fullPaths[2],
                    fullPaths[3],
                    fullPaths[4],
                    fullPaths[5]
                );

                _texture = cubeImage.CreateDeviceTexture(gd, factory);

                if ((_texture.Usage & TextureUsage.Sampled) == 0)
                {
                    var textureDesc = new TextureDescription(
                        _texture.Width,
                        _texture.Height,
                        _texture.Depth,
                        _texture.MipLevels,
                        _texture.ArrayLayers,
                        _texture.Format,
                        _texture.Usage | TextureUsage.Sampled,
                        _texture.Type
                    );

                    var newTexture = factory.CreateTexture(textureDesc);

                    using (var commandList = factory.CreateCommandList())
                    {
                        commandList.Begin();
                        commandList.CopyTexture(_texture, newTexture);
                        commandList.End();
                        gd.SubmitCommands(commandList);
                    }

                    _texture.Dispose();
                    _texture = newTexture;
                }

                _textureView = factory.CreateTextureView(_texture);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize cubemap texture resource '{Name}': {ex.Message}", ex);
            }
        }

        public TextureView GetTextureView() => _textureView;

        public override void Dispose()
        {
            _textureView?.Dispose();
            _texture?.Dispose();
        }
    }
    
    public class TextureManager : IDisposable
    {
        private static readonly Clogger LOGGER = LogManager.GetLogger("TextureManager");

        private readonly RendererBase _renderer;

        private readonly ConcurrentDictionary<string, TextureView> _views =
            new ConcurrentDictionary<string, TextureView>();

        private readonly ConcurrentDictionary<string, TextureResource> _textures2D =
            new ConcurrentDictionary<string, TextureResource>();

        private readonly ConcurrentDictionary<string, CubeMapTextureResource> _cubeMaps =
            new ConcurrentDictionary<string, CubeMapTextureResource>();

        public TextureManager(RendererBase renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public TextureView LoadTexture2D(string name, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Texture name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Texture path cannot be null or empty.", nameof(relativePath));

            if (_views.TryGetValue(name, out var existing))
                return existing;

            var texRes = new TextureResource(name, relativePath);
            _renderer.AddResource(texRes);

            var view = texRes.GetTextureView();
            _textures2D[name] = texRes;
            _views[name] = view;

            return view;
        }
        
        public TextureView LoadCubeMap(string name, string[] faces)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Texture name cannot be null or empty.", nameof(name));
            if (faces == null || faces.Length != 6)
                throw new ArgumentException("CubeMap 必须提供 6 张贴图（posX, negX, posY, negY, posZ, negZ）。", nameof(faces));

            if (_views.TryGetValue(name, out var existing))
                return existing;

            var cubeRes = new CubeMapTextureResource(name, faces);
            _renderer.AddResource(cubeRes);

            var view = cubeRes.GetTextureView();
            _cubeMaps[name] = cubeRes;
            _views[name] = view;

            LOGGER.Debug($"Loaded CubeMap '{name}' with faces: {string.Join(", ", faces)}");

            return view;
        }

        public TextureView GetTextureView(string name)
        {
            if (_views.TryGetValue(name, out var view))
                return view;

            throw new KeyNotFoundException($"Texture '{name}' not loaded in TextureManager.");
        }
        
        public bool TryGetTextureView(string name, out TextureView view)
        {
            return _views.TryGetValue(name, out view);
        }

        public void Dispose()
        {
            _views.Clear();
            _textures2D.Clear();
            _cubeMaps.Clear();
        }
    }
    
    internal static class ExrTextureLoader
    {
        public static Texture CreateTexture2D(GraphicsDevice gd, ResourceFactory factory, string fullPath)
        {
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("EXR file not found.", fullPath);

            var exrFile = EXRFile.FromFile(fullPath);
            var part = exrFile.Parts[0];

            part.OpenParallel(fullPath);

            int width = part.DataWindow.Width;
            int height = part.DataWindow.Height;

            byte[] bgraBytes = part.GetBytes(
                ImageDestFormat.PremultipliedBGRA8,
                GammaEncoding.sRGB
            );

            part.Close();

            uint w = (uint)width;
            uint h = (uint)height;

            uint mipLevels = (uint)Helpers.MipmapHelper.CalculateMipLevels(width, height);

            var desc = TextureDescription.Texture2D(
                w,
                h,
                mipLevels,
                arrayLayers: 1,
                format: PixelFormat.B8_G8_R8_A8_UNorm,
                usage: TextureUsage.Sampled | TextureUsage.GenerateMipmaps
            );

            Texture texture = factory.CreateTexture(desc);

            gd.UpdateTexture(
                texture,
                bgraBytes,
                0, 0, 0,
                w, h, 1,
                0,
                0
            );

            return texture;
        }
    }
}
