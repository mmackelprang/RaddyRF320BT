using System.Globalization;
using CsvHelper;

namespace BtMock.Logging;

/// <summary>
/// Logs received Bluetooth messages to a CSV file.
/// Each entry includes timestamp, raw data (hex and text), and user-provided description/tag.
/// </summary>
public class MessageLogger : IDisposable
{
    private readonly string _logFilePath;
    private readonly StreamWriter? _streamWriter;
    private readonly CsvWriter? _csvWriter;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new message logger with the specified file path.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    /// <param name="logFilePath">Path to the CSV log file</param>
    public MessageLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Initialize CSV writer with append mode
            _streamWriter = new StreamWriter(_logFilePath, append: true);
            _csvWriter = new CsvWriter(_streamWriter, CultureInfo.InvariantCulture);
            
            // Write header if file is new/empty
            if (new FileInfo(_logFilePath).Length == 0)
            {
                WriteHeader();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Warning: Failed to initialize CSV logger: {ex.Message}");
            // Continue without CSV logging rather than failing
        }
    }

    /// <summary>
    /// Writes the CSV header row.
    /// </summary>
    private void WriteHeader()
    {
        _csvWriter?.WriteField("Timestamp");
        _csvWriter?.WriteField("DataHex");
        _csvWriter?.WriteField("DataText");
        _csvWriter?.WriteField("ByteCount");
        _csvWriter?.WriteField("UserTag");
        _csvWriter?.NextRecord();
        _csvWriter?.Flush();
    }

    /// <summary>
    /// Logs a received message to the CSV file.
    /// </summary>
    /// <param name="timestamp">The time the message was received</param>
    /// <param name="data">The raw byte data</param>
    /// <param name="userTag">User-provided description/tag for this message</param>
    public void LogMessage(DateTime timestamp, byte[] data, string userTag)
    {
        if (_disposed || _csvWriter == null)
        {
            return;
        }

        lock (_lock)
        {
            try
            {
                // Convert data to hex string
                var hexString = BitConverter.ToString(data).Replace("-", " ");
                
                // Convert data to printable text (non-printable chars become '.')
                var textString = GetPrintableText(data);
                
                // Write record
                _csvWriter.WriteField(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                _csvWriter.WriteField(hexString);
                _csvWriter.WriteField(textString);
                _csvWriter.WriteField(data.Length);
                _csvWriter.WriteField(userTag);
                _csvWriter.NextRecord();
                _csvWriter.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Warning: Failed to write to CSV log: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Converts byte array to printable text.
    /// Non-printable characters (outside ASCII 32-126) are shown as dots.
    /// </summary>
    private static string GetPrintableText(byte[] data)
    {
        var chars = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            chars[i] = data[i] >= 32 && data[i] <= 126 
                ? (char)data[i] 
                : '.';
        }
        return new string(chars);
    }

    /// <summary>
    /// Disposes of the CSV writer and underlying stream.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _csvWriter?.Dispose();
            _streamWriter?.Dispose();
            _disposed = true;
        }
        
        GC.SuppressFinalize(this);
    }
}
