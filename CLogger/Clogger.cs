
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CMLS.CLogger
{
	public enum LogLevel
	{
		TRACE,
		DEBUG,
		INFO,
		WARN,
		ERROR,
		FATAL,
		OFF
	}

	public sealed class Clogger
	{
		private static readonly ConcurrentDictionary<string, Clogger> _loggers = new ConcurrentDictionary<string, Clogger>();
		private static IAppender _appender = new MemoryAppender();

		private static LogLevel _globalLevel = LogLevel.INFO;
		private static readonly object _globalLock = new object();

		private readonly string _name;
		private LogLevel? _instanceLevel = null;

		private Clogger(string name)
		{
			_name = name;
		}

		public static Clogger GetLogger(string name = "")
		{
			return _loggers.GetOrAdd(name, n => new Clogger(n));
		}

		/// <summary>
		/// 获取或设置当前logger的日志级别
		/// 如果设置为null，则使用全局级别
		/// </summary>
		public LogLevel? Level
		{
			get => _instanceLevel;
			set => _instanceLevel = value;
		}

		/// <summary>
		/// 获取当前实际生效的日志级别
		/// </summary>
		public LogLevel EffectiveLevel => _instanceLevel ?? _globalLevel;

		/// <summary>
		/// 设置全局日志级别
		/// </summary>
		public static void SetGlobalLevel(LogLevel level)
		{
			lock (_globalLock)
			{
				_globalLevel = level;
			}
		}

		/// <summary>
		/// 获取全局日志级别
		/// </summary>
		public static LogLevel GlobalLevel
		{
			get
			{
				lock (_globalLock)
				{
					return _globalLevel;
				}
			}
		}

		/// <summary>
		/// 设置Appender，如果当前是MemoryAppender，则将其内容转移到新Appender
		/// </summary>
		public static void SetAppender(IAppender appender)
		{
			lock (_globalLock)
			{
				if (_appender is MemoryAppender memoryAppender)
				{
					// 将内存中的日志转移到新Appender
					var bufferedEvents = memoryAppender.GetBufferedEvents();
					foreach (var logEvent in bufferedEvents)
					{
						appender.Append(logEvent);
					}
					memoryAppender.Clear(); // 清理内存
				}

				_appender = appender ?? throw new ArgumentNullException(nameof(appender));
			}
		}

		/// <summary>
		/// 直接切换到文件Appender，并指定目录
		/// </summary>
		public static void SwitchToFileAppender(string logDirectory = "logs")
		{
			var fileAppender = new FileAppender(logDirectory);
			SetAppender(fileAppender);
		}

		public static IAppender GetCurrentAppender()
		{
			return _appender;
		}

		public static IEnumerable<Clogger> GetAllLoggers()
		{
			return _loggers.Values.ToList();
		}

		public void Trace(string message) => Log(LogLevel.TRACE, message);
		public void Debug(string message) => Log(LogLevel.DEBUG, message);
		public void Info(string message) => Log(LogLevel.INFO, message);
		public void Warn(string message) => Log(LogLevel.WARN, message);
		public void Error(string message) => Log(LogLevel.ERROR, message);
		public void Fatal(string message) => Log(LogLevel.FATAL, message);

		public void Log(LogLevel level, string message)
		{
			if (level < EffectiveLevel) return;

			var logEvent = new LogEvent
			{
				Level = level,
				LoggerName = _name,
				Message = message,
				Timestamp = DateTime.Now
			};

			_appender.Append(logEvent);
		}
	}

	public struct LogEvent
	{
		public LogLevel Level { get; set; }
		public string LoggerName { get; set; }
		public string Message { get; set; }
		public DateTime Timestamp { get; set; }
	}

	public interface IAppender
	{
		void Append(LogEvent logEvent);
	}

	public class MemoryAppender : IAppender
	{
		private readonly ConcurrentQueue<LogEvent> _buffer = new ConcurrentQueue<LogEvent>();
		private readonly int _maxBufferSize = 10000; // 最大缓冲日志数量

		public void Append(LogEvent logEvent)
		{
			// 如果缓冲区已满，移除最旧的日志
			if (_buffer.Count >= _maxBufferSize)
			{
				_buffer.TryDequeue(out _);
			}
			_buffer.Enqueue(logEvent);
		}

		/// <summary>
		/// 获取所有缓冲的日志事件
		/// </summary>
		public IEnumerable<LogEvent> GetBufferedEvents()
		{
			return _buffer.ToArray();
		}

		/// <summary>
		/// 清空缓冲区
		/// </summary>
		public void Clear()
		{
			while (_buffer.TryDequeue(out _)) { }
		}

		/// <summary>
		/// 获取当前缓冲的日志数量
		/// </summary>
		public int BufferedCount => _buffer.Count;
	}

	public class ConsoleAppender : IAppender
	{
		private static readonly object _lock = new object();

		public void Append(LogEvent logEvent)
		{
			lock (_lock)
			{
				ConsoleColor originalColor = Console.ForegroundColor;

				switch (logEvent.Level)
				{
					case LogLevel.FATAL:
						Console.ForegroundColor = ConsoleColor.DarkRed;
						break;
					case LogLevel.ERROR:
						Console.ForegroundColor = ConsoleColor.Red;
						break;
					case LogLevel.WARN:
						Console.ForegroundColor = ConsoleColor.Yellow;
						break;
					case LogLevel.INFO:
						Console.ForegroundColor = ConsoleColor.Green;
						break;
					case LogLevel.DEBUG:
						Console.ForegroundColor = ConsoleColor.Gray;
						break;
					case LogLevel.TRACE:
						Console.ForegroundColor = ConsoleColor.DarkGray;
						break;
					default:
						Console.ForegroundColor = originalColor;
						break;
				}

				Console.WriteLine($"[{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEvent.Level,5}] [{logEvent.LoggerName}] - {logEvent.Message}");
				Console.ForegroundColor = originalColor;
			}
		}
	}

	public class FileAppender : IAppender, IDisposable
	{
		private readonly string _logPath;
		private StreamWriter _writer;
		private readonly object _lock = new object();
		private bool _disposed;
		private string _filePath;

		public FileAppender(string logPath)
		{
			_logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));

			// 判断是目录还是文件路径
			if (IsDirectoryPath(logPath))
			{
				// 如果是目录，创建目录并生成文件名
				Directory.CreateDirectory(logPath);
				string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
				_filePath = Path.Combine(logPath, $"log_{timestamp}.log");
			}
			else
			{
				// 如果是文件路径，确保目录存在
				string directory = Path.GetDirectoryName(logPath);
				if (!string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				// 如果文件已存在，添加时间戳后缀
				if (File.Exists(logPath))
				{
					string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(logPath);
					string extension = Path.GetExtension(logPath);
					string newFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
					_filePath = Path.Combine(directory, newFileName);
				}
				else
				{
					_filePath = logPath;
				}
			}

			InitializeWriter();
		}

		private bool IsDirectoryPath(string path)
		{
			// 如果路径以目录分隔符结尾，或者没有扩展名，则认为是目录
			return path.EndsWith(Path.DirectorySeparatorChar.ToString()) ||
				   path.EndsWith(Path.AltDirectorySeparatorChar.ToString()) ||
				   string.IsNullOrEmpty(Path.GetExtension(path));
		}

		private void InitializeWriter()
		{
			lock (_lock)
			{
				_writer?.Dispose();
				_writer = new StreamWriter(_filePath, false) { AutoFlush = true };
			}
		}

		public void Append(LogEvent logEvent)
		{
			lock (_lock)
			{
				if (_disposed) return;
				_writer.WriteLine($"[{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEvent.Level,5}] [{logEvent.LoggerName}] - {logEvent.Message}");

				if (logEvent.Level == LogLevel.FATAL)
				{
					_writer.Flush();
				}
			}
		}

		public void Dispose()
		{
			lock (_lock)
			{
				if (_disposed) return;
				_writer?.Dispose();
				_disposed = true;
			}
		}

		public void Flush()
		{
			lock (_lock)
			{
				_writer?.Flush();
			}
		}

		/// <summary>
		/// 获取当前使用的文件路径
		/// </summary>
		public string FilePath => _filePath;
	}

	public static class LogManager
	{
		public static Clogger GetLogger(string name = "")
		{
			return Clogger.GetLogger(name);
		}

		public static void Configure(LogLevel globalLevel, IAppender appender)
		{
			Clogger.SetAppender(appender);
			SetGlobalLevel(globalLevel);
		}

		public static void SetGlobalLevel(LogLevel level)
		{
			foreach (var logger in Clogger.GetAllLoggers())
			{
				logger.Level = level;
			}
		}

		/// <summary>
		/// 切换到文件日志，并指定存储目录
		/// </summary>
		public static void SwitchToFileLogging(string logDirectory = "logs")
		{
			Clogger.SwitchToFileAppender(logDirectory);
		}

		/// <summary>
		/// 获取当前内存中缓冲的日志数量（仅当使用MemoryAppender时有效）
		/// </summary>
		public static int GetBufferedLogCount()
		{
			if (Clogger.GetCurrentAppender() is MemoryAppender memoryAppender)
			{
				return memoryAppender.BufferedCount;
			}
			return 0;
		}

		public static void Shutdown()
		{
			try
			{
				if (Clogger.GetCurrentAppender() is IDisposable disposable)
				{
					disposable.Dispose();
				}

				if (Clogger.GetCurrentAppender() is AsyncAppender asyncAppender)
				{
					asyncAppender.Dispose();
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Log shutdown error: {ex}");
			}
		}
	}

	public class AsyncAppender : IAppender, IDisposable
	{
		private readonly IAppender _targetAppender;
		private readonly BlockingCollection<LogEvent> _logQueue;
		private readonly Thread _workerThread;
		private bool _disposed;

		public AsyncAppender(IAppender targetAppender)
		{
			_targetAppender = targetAppender ?? throw new ArgumentNullException(nameof(targetAppender));
			_logQueue = new BlockingCollection<LogEvent>(new ConcurrentQueue<LogEvent>());

			_workerThread = new Thread(ProcessQueue)
			{
				Name = "LoggerAsyncThread",
				IsBackground = true
			};
			_workerThread.Start();
		}

		public void Append(LogEvent logEvent)
		{
			if (!_disposed)
			{
				_logQueue.Add(logEvent);
			}
		}

		private void ProcessQueue()
		{
			try
			{
				foreach (var logEvent in _logQueue.GetConsumingEnumerable())
				{
					_targetAppender.Append(logEvent);
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Async logger error: {ex}");
			}
		}

		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
			_logQueue.CompleteAdding();

			try
			{
				_workerThread.Join(6000);
			}
			catch (ThreadInterruptedException) { }

			if (_targetAppender is FileAppender fileAppender)
			{
				fileAppender.Flush();
			}
			else if (_targetAppender is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}
	public class CompositeAppender : IAppender
	{
		private readonly List<IAppender> _appenders = new List<IAppender>();
		public void AddAppender(IAppender appender) => _appenders.Add(appender);
		public void Append(LogEvent logEvent)
		{
			foreach (var appender in _appenders)
				appender.Append(logEvent);
		}
	}

}