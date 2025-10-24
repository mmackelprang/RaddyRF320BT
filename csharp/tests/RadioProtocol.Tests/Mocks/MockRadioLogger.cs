using RadioProtocol.Core.Logging;

namespace RadioProtocol.Tests.Mocks;

/// <summary>
/// Mock radio logger for testing
/// </summary>
public class MockRadioLogger : IRadioLogger, IDisposable
{
    public List<string> LogEntries { get; } = new();
    public List<(byte[] data, string context)> RawDataSent { get; } = new();
    public List<(byte[] data, string context)> RawDataReceived { get; } = new();
    public List<(string messageType, object messageData, string context)> MessagesSent { get; } = new();
    public List<(string messageType, object messageData, string context)> MessagesReceived { get; } = new();
    
    private bool _disposed = false;

    public void LogRawDataSent(byte[] data, string methodName = "", string className = "")
    {
        var context = GetContext(className, methodName);
        RawDataSent.Add((data?.ToArray() ?? Array.Empty<byte>(), context));
        LogEntries.Add($"[{context}] RAW SENT: {(data != null ? Convert.ToHexString(data) : "null")}");
    }

    public void LogRawDataReceived(byte[] data, string methodName = "", string className = "")
    {
        var context = GetContext(className, methodName);
        RawDataReceived.Add((data?.ToArray() ?? Array.Empty<byte>(), context));
        LogEntries.Add($"[{context}] RAW RECEIVED: {(data != null ? Convert.ToHexString(data) : "null")}");
    }

    public void LogMessageSent(string messageType, object messageData, string methodName = "", string className = "")
    {
        var context = GetContext(className, methodName);
        MessagesSent.Add((messageType, messageData, context));
        LogEntries.Add($"[{context}] MESSAGE SENT: {messageType}");
    }

    public void LogMessageReceived(string messageType, object messageData, string methodName = "", string className = "")
    {
        var context = GetContext(className, methodName);
        MessagesReceived.Add((messageType, messageData, context));
        LogEntries.Add($"[{context}] MESSAGE RECEIVED: {messageType}");
    }

    public void LogInfo(string message, string methodName = "", string className = "")
    {
        var context = GetContext(className, methodName);
        LogEntries.Add($"[{context}] INFO: {message}");
    }

    public void LogWarning(string message, string methodName = "", string className = "")
    {
        var context = GetContext(className, methodName);
        LogEntries.Add($"[{context}] WARNING: {message}");
    }

    public void LogError(Exception? exception, string message, string methodName = "", string className = "")
    {
        var context = GetContext(className, methodName);
        LogEntries.Add($"[{context}] ERROR: {message} - {exception?.Message}");
    }

    public void LogDebug(string message, string methodName = "", string className = "")
    {
        var context = GetContext(className, methodName);
        LogEntries.Add($"[{context}] DEBUG: {message}");
    }

    public void Clear()
    {
        LogEntries.Clear();
        RawDataSent.Clear();
        RawDataReceived.Clear();
        MessagesSent.Clear();
        MessagesReceived.Clear();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }
    
    private static string GetContext(string filePath, string methodName)
    {
        var className = Path.GetFileNameWithoutExtension(filePath);
        return $"{className}.{methodName}";
    }
}