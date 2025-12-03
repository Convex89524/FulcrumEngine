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
using Fulcrum.ComprehensiveParser;
using Fulcrum.Engine.Render;
using SharpGen.Runtime;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fulcrum.Engine;

namespace Fulcrum.Launcher
{
    internal class Program : IDisposable
    {
		public static Clogger LOGGER = LogManager.GetLogger("Launcher");
		private static void init(string[] args)
		{
			var composite = new CompositeAppender();

			try
			{
				var parser = new CommandLineParser();
				parser.AddOption("gamepath", "g", typeof(string), required: true)
					  .AllowPositionalArguments(true);

				ParseResult result = parser.Parse(args);
				
				Global.GamePath = Path.GetFullPath(result.GetValue<string>("gamepath"));
				Global.GameFolderName = Path.GetFileName(Global.GamePath);
				Global.ResourcesPath = Path.Combine(Global.GamePath, "resources");
				Global.GameConfigPath = Path.Combine(Global.GamePath, "config");
				LOGGER.Info("游戏路径: " + Global.GamePath);
				LOGGER.Info("游戏根文件夹名: " + Global.GameFolderName);
				composite.AddAppender(new FileAppender(Global.GamePath+"\\logs"));
			}
			catch(Exception ex)
			{
				LOGGER.Fatal("错误: "+ex);
				composite.AddAppender(new FileAppender(@".\logs"));
				throw new Exception("命令行参数解析失败, 请检查传入的参数是否正确.");
			}
			
			composite.AddAppender(new ConsoleAppender());
			
			LogManager.Configure(LogLevel.DEBUG, composite);
			Clogger.SetGlobalLevel(LogLevel.DEBUG);
		}
		static void Main(string[] args)
        {
			LOGGER.Info("启动序列已启用...");
			init(args);
			LOGGER.Info("JIT开始预热...");
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			PrecompileAllAssemblies();
			stopwatch.Stop();
			LOGGER.Info($"JIT预热完成. 总耗时: {stopwatch.ElapsedMilliseconds} ms");

			if (!Directory.Exists(Path.Combine(Global.GamePath, "shaders")))
			{
				LOGGER.Warn("Shaders directory does not exist. Creating...");
				Directory.CreateDirectory(Path.Combine(Global.GamePath, "shaders"));
			}
			Initializer.startup();
		}
		
		
		private static void PrecompileAllAssemblies()
		{
			Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (Assembly assembly in allAssemblies)
			{
				LOGGER.Info($"预热程序集: {assembly.GetName().Name}");
				PrecompileAssemblyMethods(assembly);
			}
		}

		private static void PrecompileAssemblyMethods(Assembly assembly)
		{
			try
			{
				foreach (Type type in assembly.GetTypes())
				{
					PrecompileTypeMethods(type);
				}
			}
			catch (ReflectionTypeLoadException ex)
			{
				LOGGER.Warn($"预编译程序集 {assembly.GetName().Name} 时遇到类型加载异常: {ex.Message}");
				foreach (Type type in ex.Types.Where(t => t != null))
				{
					PrecompileTypeMethods(type);
				}
			}
			catch (Exception ex)
			{
				LOGGER.Warn($"预编译程序集 {assembly.GetName().Name} 时发生错误: {ex.Message}");
			}
		}

		private static void PrecompileTypeMethods(Type type)
		{
			foreach (MethodInfo method in type.GetMethods(
				BindingFlags.DeclaredOnly |
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.Instance |
				BindingFlags.Static))
			{
				if (method.IsAbstract || method.ContainsGenericParameters)
					continue;

				try
				{
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
				}
				catch (Exception ex)
				{
				}
			}

			foreach (Type nestedType in type.GetNestedTypes())
			{
				PrecompileTypeMethods(nestedType);
			}
		}

		public void Dispose()
		{
			LOGGER.Info("程序正在关闭...");
		}
	}
}
