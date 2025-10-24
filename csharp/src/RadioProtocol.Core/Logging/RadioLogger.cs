using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace RadioProtocol.Core.Logging;

/// <summary>
/// Implementation of enhanced radio logger
/// </summary>
public class RadioLogger : IRadioLogger
{
    private readonly ILogger<RadioLogger> _logger;

    public RadioLogger(ILogger<RadioLogger> logger)
    {
        _logger = logger;
    }

    public void LogRawDataSent(byte[] data, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "")
    {
        var context = GetContext(className, methodName);
        var hexData = Convert.ToHexString(data);
        _logger.LogInformation("[{Context}] RAW SENT: {HexData} ({Length} bytes)", context, hexData, data.Length);
    }

    public void LogRawDataReceived(byte[] data, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "")
    {
        var context = GetContext(className, methodName);
        var hexData = Convert.ToHexString(data);
        _logger.LogInformation("[{Context}] RAW RECEIVED: {HexData} ({Length} bytes)", context, hexData, data.Length);
    }

    public void LogMessageSent(string messageType, object messageData, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "")
    {
        var context = GetContext(className, methodName);
        _logger.LogInformation("[{Context}] MESSAGE SENT - Type: {MessageType}, Data: {@MessageData}", context, messageType, messageData);
    }

    public void LogMessageReceived(string messageType, object messageData, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "")
    {
        var context = GetContext(className, methodName);
        _logger.LogInformation("[{Context}] MESSAGE RECEIVED - Type: {MessageType}, Data: {@MessageData}", context, messageType, messageData);
    }

    public void LogInfo(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "")
    {
        var context = GetContext(className, methodName);
        _logger.LogInformation("[{Context}] {Message}", context, message);
    }

    public void LogWarning(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "")
    {
        var context = GetContext(className, methodName);
        _logger.LogWarning("[{Context}] {Message}", context, message);
    }

    public void LogError(Exception? exception, string message, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "")
    {
        var context = GetContext(className, methodName);
        _logger.LogError(exception, "[{Context}] {Message}", context, message);
    }

    public void LogDebug(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "")
    {
        var context = GetContext(className, methodName);
        _logger.LogDebug("[{Context}] {Message}", context, message);
    }

    private static string GetContext(string filePath, string methodName)
    {
        var className = Path.GetFileNameWithoutExtension(filePath);
        return $"{className}.{methodName}";
    }
}