using RadioProtocol.Core.Models;
using RadioProtocol.Core.Bluetooth;
using BluetoothDeviceInfo = RadioProtocol.Core.Bluetooth.DeviceInfo;
using RadioProtocol.Core.Protocol;
using RadioProtocol.Core.Logging;
using RadioProtocol.Core.Commands;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;
using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core;

/// <summary>
/// Main radio manager interface
/// </summary>
public interface IRadioManager : IDisposable
{
    event EventHandler<ConnectionInfo>? ConnectionStateChanged;
    event EventHandler<ResponsePacket>? MessageReceived;
    event EventHandler<RadioStatus>? StatusUpdated;
    event EventHandler<Models.DeviceInfo>? DeviceInfoReceived;
    
    Task<IEnumerable<BluetoothDeviceInfo>> ScanForDevicesAsync(CancellationToken cancellationToken = default);
    Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<CommandResult> SendCommandAsync(byte[] command, CancellationToken cancellationToken = default);
    
    // Simplified command methods
    Task<CommandResult> PressButtonAsync(ButtonType buttonType, CancellationToken cancellationToken = default);
    Task<CommandResult> PressNumberAsync(int number, bool longPress = false, CancellationToken cancellationToken = default);
    Task<CommandResult> AdjustVolumeAsync(bool up, CancellationToken cancellationToken = default);
    Task<CommandResult> NavigateAsync(bool up, bool longPress = false, CancellationToken cancellationToken = default);
    Task<CommandResult> SendHandshakeAsync(CancellationToken cancellationToken = default);
    
    bool IsConnected { get; }
    ConnectionInfo ConnectionStatus { get; }
    RadioStatus? CurrentStatus { get; }
    Models.DeviceInfo? DeviceInformation { get; }
}

/// <summary>
/// Main radio manager implementation
/// </summary>
public class RadioManager : IRadioManager
{
    private readonly IBluetoothConnection _bluetoothConnection;
    private readonly RadioCommandBuilder _commandBuilder;
    private readonly RadioProtocolParser _packetParser;
    private readonly IRadioLogger _logger;
    
    private readonly ConcurrentQueue<ResponsePacket> _responseQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly SemaphoreSlim _commandSemaphore = new(1, 1);
    
    private RadioStatus? _status;
    private Models.DeviceInfo? _deviceInfo;
    private volatile bool _disposed;

    public event EventHandler<ConnectionInfo>? ConnectionStateChanged;
    public event EventHandler<ResponsePacket>? MessageReceived;
    public event EventHandler<RadioStatus>? StatusUpdated;
    public event EventHandler<Models.DeviceInfo>? DeviceInfoReceived;

    public bool IsConnected => _bluetoothConnection.IsConnected;
    public RadioStatus? CurrentStatus => _status;
    public ConnectionInfo ConnectionStatus => _bluetoothConnection.ConnectionStatus;
    public Models.DeviceInfo? DeviceInformation => _deviceInfo;

    public RadioManager(IRadioLogger logger) 
        : this(logger, 
               BluetoothConnectionFactory.Create(logger),
               new RadioProtocolParser(logger),
               new RadioCommandBuilder(logger))
    {
    }

    public RadioManager(IBluetoothConnection bluetoothConnection, IRadioLogger logger)
        : this(logger,
               bluetoothConnection,
               new RadioProtocolParser(logger),
               new RadioCommandBuilder(logger))
    {
    }

    public RadioManager(
        IRadioLogger logger,
        IBluetoothConnection bluetoothConnection,
        RadioProtocolParser packetParser,
        RadioCommandBuilder commandBuilder)
    {
        _logger = logger;
        _bluetoothConnection = bluetoothConnection;
        _packetParser = packetParser;
        _commandBuilder = commandBuilder;

        _bluetoothConnection.DataReceived += OnDataReceived;
        _bluetoothConnection.ConnectionStateChanged += OnConnectionStateChanged;
    }

    public async Task<IEnumerable<BluetoothDeviceInfo>> ScanForDevicesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInfo("Starting device scan...");
        var devices = await _bluetoothConnection.ScanForDevicesAsync(cancellationToken);
        _logger.LogInfo($"Device scan complete. Found {devices.Count()} devices.");
        return devices;
    }

    public async Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Connecting to radio device: {deviceAddress}");
        
        var success = await _bluetoothConnection.ConnectAsync(deviceAddress, cancellationToken);
        
        if (success)
        {
            _logger.LogInfo("Radio connection established, sending handshake");
            // Send initial handshake
            await SendHandshakeAsync(cancellationToken);
        }
        
        return success;
    }

    public async Task DisconnectAsync()
    {
        _logger.LogInfo("Disconnecting from radio device");
        await _bluetoothConnection.DisconnectAsync();
    }

    public async Task<CommandResult> SendCommandAsync(byte[] command, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            await _commandSemaphore.WaitAsync(cancellationToken);
            
            if (!IsConnected)
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Not connected to radio device",
                    SentData = command,
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            _logger.LogRawDataSent(command);
            
            var success = await _bluetoothConnection.SendDataAsync(command, cancellationToken);
            
            stopwatch.Stop();
            
            if (success)
            {
                // Wait briefly for potential response
                await Task.Delay(50, cancellationToken);
                
                ResponsePacket? response = null;
                if (_responseQueue.TryDequeue(out var resp))
                {
                    response = resp;
                }

                return new CommandResult
                {
                    Success = true,
                    SentData = command,
                    ExecutionTime = stopwatch.Elapsed,
                    Response = response
                };
            }
            else
            {
                return new CommandResult
                {
                    Success = false,
                    ErrorMessage = "Failed to send command to device",
                    SentData = command,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command");
            stopwatch.Stop();
            
            return new CommandResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SentData = command,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        finally
        {
            _commandSemaphore.Release();
        }
    }

    public async Task<CommandResult> PressButtonAsync(ButtonType buttonType, CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Pressing button: {buttonType}");
        var command = _commandBuilder.BuildButtonCommand(buttonType);
        return await SendCommandAsync(command, cancellationToken);
    }

    public async Task<CommandResult> PressNumberAsync(int number, bool longPress = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Pressing number {number} (long press: {longPress})");
        
        var command = longPress 
            ? CommonCommands.NumberButtonLong(number, _logger)
            : CommonCommands.NumberButton(number, _logger);
            
        return await SendCommandAsync(command, cancellationToken);
    }

    public async Task<CommandResult> AdjustVolumeAsync(bool up, CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Adjusting volume: {(up ? "up" : "down")}");
        
        var command = up 
            ? CommonCommands.Volume.Up(_logger)
            : CommonCommands.Volume.Down(_logger);
            
        return await SendCommandAsync(command, cancellationToken);
    }

    public async Task<CommandResult> NavigateAsync(bool up, bool longPress = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Navigating: {(up ? "up" : "down")} (long press: {longPress})");
        
        var command = (up, longPress) switch
        {
            (true, false) => CommonCommands.Navigation.Up(_logger),
            (true, true) => CommonCommands.Navigation.UpLong(_logger),
            (false, false) => CommonCommands.Navigation.Down(_logger),
            (false, true) => CommonCommands.Navigation.DownLong(_logger)
        };
        
        return await SendCommandAsync(command, cancellationToken);
    }

    public async Task<CommandResult> SendHandshakeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInfo("Sending handshake");
        var command = _commandBuilder.BuildHandshakeCommand();
        return await SendCommandAsync(command, cancellationToken);
    }

    // Aliases for backward compatibility with tests
    public async Task<bool> SendButtonPressAsync(ButtonType buttonType, CancellationToken cancellationToken = default)
    {
        var result = await PressButtonAsync(buttonType, cancellationToken);
        return result.Success;
    }

    public async Task<bool> SendChannelCommandAsync(int channel, CancellationToken cancellationToken = default)
    {
        var result = await PressNumberAsync(channel, false, cancellationToken);
        return result.Success;
    }

    public async Task<bool> SendStatusRequestAsync(CancellationToken cancellationToken = default)
    {
        // Status requests are typically done via handshake or specific command
        var result = await SendHandshakeAsync(cancellationToken);
        return result.Success;
    }

    public async Task<bool> SendSyncRequestAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendHandshakeAsync(cancellationToken);
        return result.Success;
    }

    private void OnConnectionStateChanged(object? sender, ConnectionInfo connectionInfo)
    {
        _logger.LogInfo($"Connection state changed: {connectionInfo.State}");
        ConnectionStateChanged?.Invoke(this, connectionInfo);
    }

    private void OnDataReceived(object? sender, byte[] data)
    {
        var responsePacket = _packetParser.ParseReceivedData(data);

        if (responsePacket == null)
        {
            _logger.LogWarning("Failed to parse response packet.");
            return;
        }

        _logger.LogMessageReceived(responsePacket.PacketType.ToString(), responsePacket);

        MessageReceived?.Invoke(this, responsePacket);

        switch (responsePacket.PacketType)
        {
            case ResponsePacketType.DeviceInfo when responsePacket.ParsedData is Models.DeviceInfo deviceInfo:
                _deviceInfo = deviceInfo;
                DeviceInfoReceived?.Invoke(this, _deviceInfo);
                break;
            case ResponsePacketType.FrequencyStatus when responsePacket.ParsedData is RadioStatus status:
                _status = status;
                StatusUpdated?.Invoke(this, _status);
                break;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _bluetoothConnection?.Dispose();
                _commandSemaphore?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Builder for RadioManager with fluent configuration
/// </summary>
public class RadioManagerBuilder
{
    private IRadioLogger? _logger;
    private string? _logFilePath;

    public RadioManagerBuilder WithLogger(IRadioLogger logger)
    {
        _logger = logger;
        return this;
    }

    public RadioManagerBuilder WithFileLogging(string logFilePath)
    {
        _logFilePath = logFilePath;
        return this;
    }

    public IRadioManager Build()
    {
        // Create logger if none provided
        if (_logger == null)
        {
            // Create a simple wrapper that implements ILogger<RadioLogger>
            var simpleLogger = new SimpleLoggerWrapper(_logFilePath);
            _logger = new RadioLogger(simpleLogger);
        }

        var bluetoothConnection = BluetoothConnectionFactory.Create(_logger);
        var packetParser = new RadioProtocolParser(_logger);
        var commandBuilder = new RadioCommandBuilder(_logger);

        return new RadioManager(_logger, bluetoothConnection, packetParser, commandBuilder);
    }
}

/// <summary>
/// Simple logger wrapper that implements ILogger<RadioLogger> and uses FileLogger for file output
/// </summary>
internal class SimpleLoggerWrapper : Microsoft.Extensions.Logging.ILogger<RadioLogger>
{
    private readonly FileLogger? _fileLogger;
    private static readonly object _cleanupLock = new();
    private static DateTime _lastCleanup = DateTime.MinValue;

    public SimpleLoggerWrapper(string? logFilePath)
    {
        if (!string.IsNullOrEmpty(logFilePath))
        {
            var dailyFilePath = GetDailyLogFilePath(logFilePath);
            _fileLogger = new FileLogger("RadioLogger", dailyFilePath, Microsoft.Extensions.Logging.LogLevel.Information);
            
            // Perform cleanup if needed
            CleanupOldLogFiles(logFilePath);
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => new NullDisposable();

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => _fileLogger?.IsEnabled(logLevel) ?? true;

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _fileLogger?.Log(logLevel, eventId, state, exception, formatter);
        
        // Also log to console for development
        var message = formatter(state, exception);
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        System.Console.WriteLine($"[{timestamp}] {message}");
    }

    private static string GetDailyLogFilePath(string baseFilePath)
    {
        var directory = Path.GetDirectoryName(baseFilePath) ?? "";
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFilePath);
        var extension = Path.GetExtension(baseFilePath);
        var dateString = DateTime.Now.ToString("yyyy-MM-dd");
        
        // Ensure directory exists
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        return Path.Combine(directory, $"{fileNameWithoutExtension}_{dateString}{extension}");
    }

    private static void CleanupOldLogFiles(string baseFilePath)
    {
        lock (_cleanupLock)
        {
            // Only run cleanup once per day
            if (DateTime.Now.Date == _lastCleanup.Date)
                return;

            try
            {
                var directory = Path.GetDirectoryName(baseFilePath);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    return;

                var fileNamePattern = Path.GetFileNameWithoutExtension(baseFilePath);
                var extension = Path.GetExtension(baseFilePath);
                var cutoffDate = DateTime.Now.AddDays(-2).Date;

                var filesToDelete = Directory.GetFiles(directory, $"{fileNamePattern}_*{extension}")
                    .Where(file =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var lastUnderscoreIndex = fileName.LastIndexOf('_');
                        if (lastUnderscoreIndex == -1) return false;
                        
                        var dateString = fileName.Substring(lastUnderscoreIndex + 1);
                        
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

    private class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}