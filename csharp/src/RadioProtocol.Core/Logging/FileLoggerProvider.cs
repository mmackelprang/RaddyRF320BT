using Microsoft.Extensions.Logging;

namespace RadioProtocol.Core.Logging;

/// <summary>
/// File logger provider for cross-platform file logging with daily rotation and garbage collection
/// </summary>
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _baseFilePath;
    private readonly LogLevel _minLevel;
    private static readonly object _cleanupLock = new();
    private static DateTime _lastCleanup = DateTime.MinValue;

    public FileLoggerProvider(string baseFilePath, LogLevel minLevel = LogLevel.Information)
    {
        _baseFilePath = baseFilePath;
        _minLevel = minLevel;
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(baseFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Perform cleanup if needed
        CleanupOldLogFiles();
    }

    public ILogger CreateLogger(string categoryName)
    {
        var dailyFilePath = GetDailyLogFilePath();
        return new FileLogger(categoryName, dailyFilePath, _minLevel);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private string GetDailyLogFilePath()
    {
        var directory = Path.GetDirectoryName(_baseFilePath) ?? "";
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(_baseFilePath);
        var extension = Path.GetExtension(_baseFilePath);
        var dateString = DateTime.Now.ToString("yyyy-MM-dd");
        
        return Path.Combine(directory, $"{fileNameWithoutExtension}_{dateString}{extension}");
    }

    private void CleanupOldLogFiles()
    {
        lock (_cleanupLock)
        {
            // Only run cleanup once per day
            if (DateTime.Now.Date == _lastCleanup.Date)
                return;

            try
            {
                var directory = Path.GetDirectoryName(_baseFilePath);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    return;

                var fileNamePattern = Path.GetFileNameWithoutExtension(_baseFilePath);
                var extension = Path.GetExtension(_baseFilePath);
                var cutoffDate = DateTime.Now.AddDays(-2).Date;

                var filesToDelete = Directory.GetFiles(directory, $"{fileNamePattern}_*{extension}")
                    .Where(file =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var dateString = fileName.Substring(fileName.LastIndexOf('_') + 1);
                        
                        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, 
                            System.Globalization.DateTimeStyles.None, out var fileDate))
                        {
                            return fileDate < cutoffDate;
                        }
                        return false;
                    });

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }

                _lastCleanup = DateTime.Now;
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}