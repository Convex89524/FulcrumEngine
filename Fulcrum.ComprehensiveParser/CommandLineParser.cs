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

namespace Fulcrum.ComprehensiveParser
{
	/// <summary>
	/// 命令行参数解析结果
	/// </summary>
	public class ParseResult
	{
		private readonly Dictionary<string, object> _parsedValues = new();
		private readonly List<string> _positionalArgs = new();

		public IReadOnlyList<string> PositionalArguments => _positionalArgs.AsReadOnly();

		public void AddOption(string key, object value) => _parsedValues[key] = value;
		public void AddPositionalArgument(string value) => _positionalArgs.Add(value);

		public T GetValue<T>(string key) => _parsedValues.TryGetValue(key, out object value) ? (T)value : default;
		public bool TryGetValue<T>(string key, out T result)
		{
			if (_parsedValues.TryGetValue(key, out object value) && value is T typedValue)
			{
				result = typedValue;
				return true;
			}
			result = default;
			return false;
		}

		public bool ContainsKey(string key) => _parsedValues.ContainsKey(key);
	}

	/// <summary>
	/// 参数定义配置
	/// </summary>
	public class OptionDefinition
	{
		public string Name { get; set; }
		public string ShortName { get; set; }
		public Type ValueType { get; set; } = typeof(bool);
		public bool IsRequired { get; set; }
		public object DefaultValue { get; set; }
	}

	/// <summary>
	/// 命令行解析器
	/// </summary>
	public class CommandLineParser
	{
		private readonly List<OptionDefinition> _options = new();
		private bool _allowPositionalArguments = true;

		public CommandLineParser AllowPositionalArguments(bool allow)
		{
			_allowPositionalArguments = allow;
			return this;
		}

		public CommandLineParser AddOption(string name, string shortName = null,
										  Type type = null, bool required = false,
										  object defaultValue = null)
		{
			_options.Add(new OptionDefinition
			{
				Name = name,
				ShortName = shortName,
				ValueType = type ?? typeof(bool),
				IsRequired = required,
				DefaultValue = defaultValue
			});
			return this;
		}

		public ParseResult Parse(string[] args)
		{
			var result = new ParseResult();
			var optionDefinitions = _options.ToDictionary(o => o.Name);
			var shortNameMap = _options.Where(o => !string.IsNullOrEmpty(o.ShortName))
									  .ToDictionary(o => o.ShortName, o => o.Name);

			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];

				// 处理长选项 (--option)
				if (arg.StartsWith("--"))
				{
					ProcessLongOption(arg, args, ref i, result, optionDefinitions);
				}
				// 处理短选项 (-o)
				else if (arg.StartsWith("-") && arg.Length > 1)
				{
					ProcessShortOption(arg, args, ref i, result, shortNameMap, optionDefinitions);
				}
				// 处理键值对选项 (--key=value)
				else if (arg.Contains('='))
				{
					ProcessKeyValueOption(arg, result, optionDefinitions);
				}
				// 位置参数
				else if (_allowPositionalArguments)
				{
					result.AddPositionalArgument(arg);
				}
				else
				{
					throw new ArgumentException($"Unexpected argument: {arg}");
				}
			}

			ValidateRequiredOptions(result, optionDefinitions);
			return result;
		}

		private void ProcessLongOption(string arg, string[] args, ref int index,
									  ParseResult result,
									  Dictionary<string, OptionDefinition> options)
		{
			string optionName = arg.Substring(2);
			string value = null;

			// 检查是否包含等号 (--option=value)
			if (optionName.Contains('='))
			{
				var parts = optionName.Split('=', 2);
				optionName = parts[0];
				value = parts[1];
			}

			if (!options.TryGetValue(optionName, out OptionDefinition def))
				throw new ArgumentException($"Unknown option: {optionName}");

			ProcessOptionValue(optionName, value, args, ref index, result, def);
		}

		private void ProcessShortOption(string arg, string[] args, ref int index,
									   ParseResult result,
									   Dictionary<string, string> shortNameMap,
									   Dictionary<string, OptionDefinition> options)
		{
			string shortName = arg.Substring(1, 1);
			if (!shortNameMap.TryGetValue(shortName, out string fullName))
				throw new ArgumentException($"Unknown option: -{shortName}");

			var def = options[fullName];
			string value = arg.Length > 2 ? arg.Substring(2) : null;

			ProcessOptionValue(fullName, value, args, ref index, result, def);
		}

		private void ProcessKeyValueOption(string arg, ParseResult result,
										 Dictionary<string, OptionDefinition> options)
		{
			var parts = arg.Split('=', 2);
			string key = parts[0];
			string value = parts[1];

			if (!options.TryGetValue(key, out OptionDefinition def))
				throw new ArgumentException($"Unknown option: {key}");

			result.AddOption(def.Name, ConvertValue(value, def.ValueType));
		}

		private void ProcessOptionValue(string name, string currentValue, string[] args, ref int index,
									  ParseResult result, OptionDefinition def)
		{
			// 布尔类型特殊处理
			if (def.ValueType == typeof(bool))
			{
				result.AddOption(name, true);
				return;
			}

			// 从当前参数或下一个参数获取值
			string value = currentValue;
			if (string.IsNullOrEmpty(value))
			{
				if (index + 1 >= args.Length || args[index + 1].StartsWith("-"))
				{
					if (def.DefaultValue != null)
					{
						result.AddOption(name, def.DefaultValue);
						return;
					}
					throw new ArgumentException($"Missing value for option: {name}");
				}
				value = args[++index];
			}

			result.AddOption(name, ConvertValue(value, def.ValueType));
		}

		private object ConvertValue(string value, Type targetType)
		{
			try
			{
				if (targetType == typeof(string)) return value;
				if (targetType == typeof(int)) return int.Parse(value);
				if (targetType == typeof(double)) return double.Parse(value);
				if (targetType == typeof(bool)) return bool.Parse(value);
				if (targetType == typeof(DateTime)) return DateTime.Parse(value);
				if (targetType.IsEnum) return Enum.Parse(targetType, value, true);

				throw new NotSupportedException($"Unsupported type: {targetType}");
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"Value conversion failed for '{value}': {ex.Message}");
			}
		}

		private void ValidateRequiredOptions(ParseResult result,
											Dictionary<string, OptionDefinition> options)
		{
			foreach (var def in options.Values.Where(o => o.IsRequired))
			{
				if (!result.ContainsKey(def.Name))
				{
					throw new ArgumentException($"Required option missing: {def.Name}");
				}
			}
		}
	}
}
