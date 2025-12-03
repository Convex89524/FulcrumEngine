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

using Fulcrum.Engine.Render;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace Fulcrum.Engine.Object
{
	public static class TextureResourceManager
	{
		private static readonly ConcurrentDictionary<string, string> _texturePathMap = new ConcurrentDictionary<string, string>();
		private static readonly ConcurrentDictionary<string, TextureResource> _textureResources = new ConcurrentDictionary<string, TextureResource>();
		private static readonly object _lock = new object();

		/// <summary>
		/// 注册纹理资源
		/// </summary>
		public static bool RegisterTexture(TextureResource textureResource)
		{
			if (textureResource == null || string.IsNullOrEmpty(textureResource.Name))
				return false;

			lock (_lock)
			{
				_textureResources[textureResource.Name] = textureResource;
				return true;
			}
		}

		/// <summary>
		/// 注册纹理路径
		/// </summary>
		public static bool RegisterTexturePath(string textureName, string filePath)
		{
			if (string.IsNullOrEmpty(textureName) || string.IsNullOrEmpty(filePath))
				return false;

			lock (_lock)
			{
				_texturePathMap[textureName] = filePath;
				return true;
			}
		}

		/// <summary>
		/// 获取纹理路径
		/// </summary>
		public static bool TryGetTexturePath(string textureName, out string filePath)
		{
			lock (_lock)
			{
				return _texturePathMap.TryGetValue(textureName, out filePath);
			}
		}

		/// <summary>
		/// 获取纹理资源
		/// </summary>
		public static bool TryGetTextureResource(string textureName, out TextureResource textureResource)
		{
			lock (_lock)
			{
				return _textureResources.TryGetValue(textureName, out textureResource);
			}
		}

		/// <summary>
		/// 通过纹理对象获取纹理路径
		/// </summary>
		public static bool TryGetTexturePath(Texture texture, out string filePath)
		{
			filePath = null;

			if (texture == null)
				return false;

			lock (_lock)
			{
				if (!string.IsNullOrEmpty(texture.Name) && _texturePathMap.TryGetValue(texture.Name, out filePath))
					return true;

				foreach (var resource in _textureResources.Values)
				{
					var textureField = typeof(TextureResource).GetField("_texture",
						BindingFlags.NonPublic | BindingFlags.Instance);

					if (textureField != null)
					{
						var resourceTexture = textureField.GetValue(resource) as Texture;
						if (resourceTexture == texture)
						{
							var filePathField = typeof(TextureResource).GetField("_filePath",
								BindingFlags.NonPublic | BindingFlags.Instance);

							if (filePathField != null)
							{
								filePath = filePathField.GetValue(resource) as string;
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// 通过纹理视图获取纹理路径
		/// </summary>
		public static bool TryGetTexturePath(TextureView textureView, out string filePath)
		{
			filePath = null;

			if (textureView == null)
				return false;

			var targetProperty = typeof(TextureView).GetProperty("Target",
				BindingFlags.Public | BindingFlags.Instance);

			if (targetProperty != null)
			{
				var texture = targetProperty.GetValue(textureView) as Texture;
				return TryGetTexturePath(texture, out filePath);
			}

			return false;
		}

		/// <summary>
		/// 移除纹理注册
		/// </summary>
		public static bool UnregisterTexture(string textureName)
		{
			lock (_lock)
			{
				bool removedPath = _texturePathMap.TryRemove(textureName, out _);
				bool removedResource = _textureResources.TryRemove(textureName, out _);
				return removedPath || removedResource;
			}
		}

		/// <summary>
		/// 清除所有纹理注册
		/// </summary>
		public static void Clear()
		{
			lock (_lock)
			{
				_texturePathMap.Clear();
				_textureResources.Clear();
			}
		}

		/// <summary>
		/// 获取所有注册的纹理名称
		/// </summary>
		public static IEnumerable<string> GetAllTextureNames()
		{
			lock (_lock)
			{
				return _texturePathMap.Keys.ToList();
			}
		}

		/// <summary>
		/// 获取所有注册的纹理路径
		/// </summary>
		public static IEnumerable<string> GetAllTexturePaths()
		{
			lock (_lock)
			{
				return _texturePathMap.Values.ToList();
			}
		}

		/// <summary>
		/// 检查纹理是否已注册
		/// </summary>
		public static bool IsTextureRegistered(string textureName)
		{
			lock (_lock)
			{
				return _texturePathMap.ContainsKey(textureName) || _textureResources.ContainsKey(textureName);
			}
		}

		/// <summary>
		/// 获取纹理数量
		/// </summary>
		public static int GetTextureCount()
		{
			lock (_lock)
			{
				return Math.Max(_texturePathMap.Count, _textureResources.Count);
			}
		}
	}
}
