using Microsoft.Extensions.Logging;

namespace RadioProtocol.Core.Logging;

/// <summary>
/// File logger implementation with thread-safe file writing
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _filePath;
    private readonly LogLevel _minLevel;
    private static readonly object _lock = new();

    public FileLogger(string categoryName, string filePath, LogLevel minLevel)
    {
        _categoryName = categoryName;
        _filePath = filePath;
        _minLevel = minLevel;
    }

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = $"[{timestamp}] [{logLevel}] [{_categoryName}] {message}";
        
        if (exception != null)
        {
            logEntry += Environment.NewLine + exception.ToString();
        }

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_filePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Ignore file write errors to prevent logging from breaking the application
            }
        }
    }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}