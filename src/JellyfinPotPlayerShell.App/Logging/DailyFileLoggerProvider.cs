using System.Collections.Concurrent;
using System.Text;
using System.IO;
using Microsoft.Extensions.Logging;

namespace JellyfinPotPlayerShell.App.Logging;

public sealed class DailyFileLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DailyFileLogger> _loggers = new();
    private readonly string _logDirectory;
    private readonly object _writeLock = new();

    public DailyFileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DailyFileLogger(name, WriteLine));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    private void WriteLine(string line)
    {
        lock (_writeLock)
        {
            Directory.CreateDirectory(_logDirectory);
            var logPath = Path.Combine(_logDirectory, $"jpps-{DateTimeOffset.Now:yyyy-MM-dd}.log");
            File.AppendAllText(logPath, line + Environment.NewLine, new UTF8Encoding(false));
        }
    }

    private sealed class DailyFileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly Action<string> _writeLine;

        public DailyFileLogger(string categoryName, Action<string> writeLine)
        {
            _categoryName = categoryName;
            _writeLine = writeLine;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            var exceptionText = exception is null ? string.Empty : $"{Environment.NewLine}{exception}";
            _writeLine($"{DateTimeOffset.Now:O} [{logLevel}] {_categoryName}: {message}{exceptionText}");
        }
    }
}
