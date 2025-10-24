using System.Runtime.CompilerServices;

namespace RadioProtocol.Core.Logging;

/// <summary>
/// Enhanced logger with automatic class and method context
/// </summary>
public interface IRadioLogger
{
    void LogRawDataSent(byte[] data, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "");
    void LogRawDataReceived(byte[] data, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "");
    void LogMessageSent(string messageType, object messageData, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "");
    void LogMessageReceived(string messageType, object messageData, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "");
    void LogInfo(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "");
    void LogWarning(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "");
    void LogError(Exception? exception, string message, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "");
    void LogDebug(string message, [CallerMemberName] string methodName = "", [CallerFilePath] string className = "");
}