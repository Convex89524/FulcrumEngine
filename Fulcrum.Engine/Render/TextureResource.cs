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
using Fulcrum.Common;
using Fulcrum.DefaultResources;
using Veldrid;
using Veldrid.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Fulcrum.Engine.Render
{
    public class TextureResource : RenderResource
    {
        private Texture _texture;
        private TextureView _textureView;
        private string _filePath;
        public string FilePath => _filePath;
        private Clogger LOGGER = LogManager.GetLogger("Render");

        private const string EmbeddedAssemblyName = "Fulcrum.DefaultResources";
        private static readonly string[] EmbeddedTextureNamespaces =
        {
            "Fl.DefaultResources.Texture",
            "Fulcrum.DefaultResources.Texture"
        };

        public TextureResource(string name, string filePath)
        {
            Name = name;
            _filePath = filePath;
        }

        public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
        {
            string fullPath = Path.Combine(Global.ResourcesPath, "textures", _filePath);

            try
            {
                ImageSharpTexture image = null;
                
                string ext = Path.GetExtension(fullPath).ToLowerInvariant();
                
                if (File.Exists(fullPath))
                {
                    LOGGER.Info($"Texture loaded from file: {fullPath}");
                    if (ext == ".exr")
                    {
                        _texture = ExrTextureLoader.CreateTexture2D(gd, factory, fullPath);
                    }
                    else
                    {
                        image = new ImageSharpTexture(fullPath);
                        _texture = image.CreateDeviceTexture(gd, factory);
                    }
                }
                else if (TryLoadEmbeddedTexture(out image))
                {
                    LOGGER.Info($"Texture '{Name}' loaded from embedded resources (assembly: {EmbeddedAssemblyName}).");
                }
                else
                {
                    LOGGER.Warn($"Texture not found: {fullPath}, using embedded fallback texture.");
                    if (!TryLoadSpecificEmbedded("_null", out image))
                        throw new Exception("Fallback texture not found in assembly!");
                }

                _texture = image.CreateDeviceTexture(gd, factory);

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
                throw new Exception($"Failed to initialize texture resource '{Name}' from file '{_filePath}': {ex.Message}");
            }
        }

        private bool TryLoadEmbeddedTexture(out ImageSharpTexture texture)
        {
            texture = null;
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == EmbeddedAssemblyName);

                if (asm == null)
                {
                    LOGGER.Warn($"Embedded resource assembly '{EmbeddedAssemblyName}' not found.");
                    return false;
                }

                var resourceKey = Path.GetFileNameWithoutExtension(_filePath);
                var candidates = EmbeddedTextureNamespaces
                    .Select(ns => $"{ns}.{resourceKey}.png")
                    .Concat(new[] { resourceKey, resourceKey + ".png" })
                    .ToArray();

                foreach (var resName in candidates)
                {
                    using (Stream stream = asm.GetManifestResourceStream(resName))
                    {
                        if (stream == null)
                            continue;

                        LOGGER.Info($"Found embedded texture resource: {resName}");
                        var img = Image.Load<Rgba32>(stream);
                        texture = new ImageSharpTexture(img, true);
                        return true;
                    }
                }

                LOGGER.Warn($"No embedded texture resource found for key '{resourceKey}' in assembly '{EmbeddedAssemblyName}'.");
                return false;
            }
            catch (Exception ex)
            {
                LOGGER.Error($"Error while loading embedded texture for '{Name}': {ex.Message}");
                return false;
            }
        }

        private bool TryLoadSpecificEmbedded(string resourceName, out ImageSharpTexture texture)
        {
            texture = null;
            try
            {
                texture = ResourceLoader.LoadFromResx(resourceName);
                return true;
            }
            catch (Exception ex)
            {
                LOGGER.Error($"Error loading specific embedded texture '{resourceName}': {ex.Message}");
                return false;
            }
        }

        public TextureView GetTextureView() => _textureView;

        public override void Dispose()
        {
            _textureView?.Dispose();
            _texture?.Dispose();
        }
    }
}
