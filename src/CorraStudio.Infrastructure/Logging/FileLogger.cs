using Microsoft.Extensions.Logging;
using System.IO;

namespace CorraStudio.Infrastructure.Logging;

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logPath;

    public FileLogger(string categoryName, string logPath)
    {
        _categoryName = categoryName;
        _logPath = logPath;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return default!;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] [{_categoryName}] {message}";
        
        if (exception != null)
            logEntry += $"\nException: {exception}";

        try
        {
            var logDirectory = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            File.AppendAllText(_logPath, logEntry + Environment.NewLine);
        }
        catch
        {
            // Silently fail if cannot write to log
        }
    }
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logPath;

    public FileLoggerProvider(string logPath)
    {
        _logPath = logPath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _logPath);
    }

    public void Dispose()
    {
    }
}

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, string logPath)
    {
        builder.AddProvider(new FileLoggerProvider(logPath));
        return builder;
    }
}
