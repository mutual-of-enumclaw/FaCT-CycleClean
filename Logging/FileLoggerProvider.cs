using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Fact.BatchCleaner.Logging
{
    /// <summary>
    /// A shared file writer that handles concurrent access to a single log file from multiple logger instances.
    /// </summary>
    public class SharedFileWriter : IDisposable
    {
        private static readonly object _instanceLock = new object();
        private static SharedFileWriter? _instance;
        private readonly object _writeLock = new object();
        private StreamWriter? _streamWriter;
        private string? _currentLogFile;
        private readonly LoggingSettings _settings;
        private bool _disposed = false;

        private SharedFileWriter(LoggingSettings settings)
        {
            _settings = settings;
        }

        public static SharedFileWriter GetInstance(LoggingSettings settings)
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new SharedFileWriter(settings);
                    }
                }
            }
            return _instance;
        }

        public void WriteLogMessage(string message)
        {
            if (_disposed) return;

            lock (_writeLock)
            {
                try
                {
                    EnsureStreamWriter();
                    _streamWriter?.WriteLine(message);
                    _streamWriter?.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FILE LOGGING ERROR] {ex.Message}");
                    Console.WriteLine($"[FALLBACK] {message}");
                }
            }
        }

        private void EnsureStreamWriter()
        {
            var logFilePath = _settings.GetLogFilePath();
            
            if (_currentLogFile != logFilePath)
            {
                _streamWriter?.Dispose();
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[DEBUG] Created log directory: {directory}");
                }
                
                try
                {
                    _streamWriter = new StreamWriter(logFilePath, append: true)
                    {
                        AutoFlush = true
                    };
                    _currentLogFile = logFilePath;
                    
                    Console.WriteLine($"[DEBUG] Shared log file opened for this application run: {logFilePath}");
                    
                    // Write a startup marker to the file
                    _streamWriter.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [INFO] === FaCT Cycle Cleaner Log Started ===");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to create log file at {logFilePath}: {ex.Message}");
                    throw;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_writeLock)
                {
                    _streamWriter?.Dispose();
                    _disposed = true;
                }
            }
        }
    }

    /// <summary>
    /// A custom file logger provider that creates file loggers with configurable settings.
    /// </summary>
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly LoggingSettings _settings;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        private readonly SharedFileWriter _sharedWriter;

        public FileLoggerProvider(IOptions<LoggingSettings> settings)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            
            Console.WriteLine($"[FILELOGGER DEBUG] FileLoggerProvider created");
            Console.WriteLine($"[FILELOGGER DEBUG] Log Directory: {_settings.LogDirectory}");
            Console.WriteLine($"[FILELOGGER DEBUG] Log File Name: {_settings.LogFileName}");
            
            EnsureLogDirectoryExists();
            CleanupOldLogFiles();
            
            // Initialize the shared file writer
            _sharedWriter = SharedFileWriter.GetInstance(_settings);
            
            Console.WriteLine($"[FILELOGGER DEBUG] FileLoggerProvider initialization complete");
        }

        public ILogger CreateLogger(string categoryName)
        {
            Console.WriteLine($"[FILELOGGER DEBUG] Creating logger for category: {categoryName}");
            return _loggers.GetOrAdd(categoryName, name => 
            {
                Console.WriteLine($"[FILELOGGER DEBUG] New FileLogger created for: {name}");
                return new FileLogger(name, _settings, _sharedWriter);
            });
        }

        public void Dispose()
        {
            foreach (var logger in _loggers.Values)
            {
                logger.Dispose();
            }
            _loggers.Clear();
            _sharedWriter?.Dispose();
        }

        private void EnsureLogDirectoryExists()
        {
            Console.WriteLine($"[FILELOGGER DEBUG] Checking if log directory exists: {_settings.LogDirectory}");
            
            if (!Directory.Exists(_settings.LogDirectory))
            {
                Console.WriteLine($"[FILELOGGER DEBUG] Creating log directory: {_settings.LogDirectory}");
                Directory.CreateDirectory(_settings.LogDirectory);
                Console.WriteLine($"[FILELOGGER DEBUG] Log directory created successfully");
            }
            else
            {
                Console.WriteLine($"[FILELOGGER DEBUG] Log directory already exists");
            }
        }

        private void CleanupOldLogFiles()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-_settings.RetainDays);
                var logFiles = Directory.GetFiles(_settings.LogDirectory, "*.log");

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        fileInfo.Delete();
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors - don't let them stop the application
            }
        }
    }

    /// <summary>
    /// A custom file logger that writes log messages to files with configurable formatting.
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string _categoryName;
        private readonly LoggingSettings _settings;
        private readonly SharedFileWriter _sharedWriter;

        public FileLogger(string categoryName, LoggingSettings settings, SharedFileWriter sharedWriter)
        {
            _categoryName = categoryName;
            _settings = settings;
            _sharedWriter = sharedWriter;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel)
        {
            // Allow all messages at Warning level and above for Fact.BatchCleaner
            if (_categoryName.StartsWith("Fact.BatchCleaner"))
            {
                return logLevel >= LogLevel.Warning;
            }
            
            // For Program category (startup messages), allow Warning and above
            if (_categoryName.Contains("Program"))
            {
                return logLevel >= LogLevel.Warning;
            }

            // For other categories (Microsoft, System), only show Error and above
            return logLevel >= LogLevel.Error;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                var formattedMessage = FormatLogMessage(logLevel, message, exception);
                _sharedWriter.WriteLogMessage(formattedMessage);
                
                // Exception details should also be written to the file if present
                if (exception != null)
                {
                    var exceptionMessage = FormatLogMessage(LogLevel.Error, $"Exception: {exception}", null);
                    _sharedWriter.WriteLogMessage(exceptionMessage);
                }
            }
            catch (Exception ex)
            {
                // If file logging fails, output to console as emergency fallback only
                Console.WriteLine($"[FILE LOGGING ERROR] {ex.Message}");
                Console.WriteLine($"[FALLBACK] {DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {message}");
            }
        }

        private string FormatLogMessage(LogLevel logLevel, string message, Exception? exception)
        {
            var timestamp = _settings.IncludeTimestamps ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] " : "";
            var level = $"[{logLevel.ToString().ToUpper()}] ";
            var category = _settings.VerboseLogging ? $"[{_categoryName}] " : "";

            return $"{timestamp}{level}{category}{message}";
        }

        public void Dispose()
        {
            // Nothing to dispose at the individual logger level since we use a shared writer
        }
    }
}