using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace RadioClient;

public enum MessageSource
{
    Radio,
    Application
}

public sealed class MessageLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new object();
    private readonly string _logFilePath;
    private bool _isDisposed;

    public string LogFilePath => _logFilePath;

    private MessageLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        _writer = new StreamWriter(logFilePath, append: true, Encoding.UTF8) { AutoFlush = true };
    }

    public static MessageLogger Create(string? customPath = null)
    {
        var logDir = customPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RadioClient", "Logs");
        Directory.CreateDirectory(logDir);
        
        var fileName = $"RadioClient_{DateTime.Now:yyyyMMdd_HHmmss}.log";
        var logPath = Path.Combine(logDir, fileName);
        
        var logger = new MessageLogger(logPath);
        logger.LogHeader();
        return logger;
    }

    private void LogHeader()
    {
        lock (_lock)
        {
            _writer.WriteLine("=".PadRight(80, '='));
            _writer.WriteLine($"Radio Client Test Session Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            _writer.WriteLine("=".PadRight(80, '='));
            _writer.WriteLine();
        }
    }

    public void LogMessage(MessageSource source, byte[] data, string? messageType = null)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var hexData = BitConverter.ToString(data).Replace("-", " ");
            var type = messageType ?? "Unknown";
            
            _writer.WriteLine($"[{timestamp}] {source,-11} | Type: {type,-20} | Data: {hexData}");
        }
    }

    public void LogFrame(MessageSource source, RadioFrame frame)
    {
        if (_isDisposed) return;

        var messageType = GetMessageType(frame);
        var data = frame.ToBytes();
        LogMessage(source, data, messageType);
    }

    public void LogState(RadioState state)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _writer.WriteLine($"[{timestamp}] {"Radio",-11} | Type: {"State Update",-20} | Freq: {state.FrequencyMHz:0.00000} MHz ({(state.UnitIsMHz ? "MHz" : "KHz")})");
            _writer.WriteLine($"{"",25} | Raw Hex: {state.RawHex}");
        }
    }

    public void LogInfo(string message)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _writer.WriteLine($"[{timestamp}] {"INFO",-11} | {message}");
        }
    }

    public void LogError(string message)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            _writer.WriteLine($"[{timestamp}] {"ERROR",-11} | {message}");
        }
    }

    private string GetMessageType(RadioFrame frame)
    {
        // Check if it's a handshake
        if (frame.Header == 0xAB && frame.Proto == 0x01)
        {
            return "Handshake";
        }

        // Check for acknowledgments
        if (frame.Group == CommandGroup.Ack)
        {
            return frame.CommandId switch
            {
                0x00 => "Ack: Fail",
                0x01 => "Ack: Success",
                _ => $"Ack: Unknown (0x{frame.CommandId:X2})"
            };
        }

        // Check for button commands
        if (frame.Group == CommandGroup.Button)
        {
            var action = CommandIdMap.Id.FirstOrDefault(kvp => kvp.Value == frame.CommandId).Key;
            return action != default ? $"Button: {action}" : $"Button: Unknown (0x{frame.CommandId:X2})";
        }
        
        // Check for status messages
        if (frame.Group == CommandGroup.Status)
        {
            return $"Status: 0x{frame.CommandId:X2}";
        }

        return $"Group: {frame.Group} (0x{frame.CommandId:X2})";
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        lock (_lock)
        {
            _writer.WriteLine();
            _writer.WriteLine("=".PadRight(80, '='));
            _writer.WriteLine($"Session Ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            _writer.WriteLine("=".PadRight(80, '='));
            _writer.Dispose();
        }
    }
}
