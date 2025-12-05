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
using Radio.Core.Interfaces.Audio;
using Radio.Core.Models.Audio;

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
/// Main radio manager implementation that provides both radio protocol management
/// and radio control functionality.
/// </summary>
/// <remarks>
/// This class implements <see cref="IRadioManager"/> for radio protocol management
/// and <see cref="IRadioControls"/> for radio-specific controls.
/// The IRadioControls implementation is currently stubbed out and will throw
/// <see cref="NotImplementedException"/> for all IRadioControls members.
/// These methods are marked with TODO comments for future implementation.
/// </remarks>
public class RadioManager : IRadioManager, IRadioControls
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
            case ResponsePacketType.TextMessage when responsePacket.ParsedData is TextMessageInfo textMsg:
                // Handle multi-part text messages (e.g., model name, version info)
                if (textMsg.IsComplete && !string.IsNullOrEmpty(textMsg.Message))
                {
                    _logger.LogInfo($"Text message complete: {textMsg.Message}");
                    // Update device info with the assembled text message
                    _deviceInfo ??= new Models.DeviceInfo();
                    _deviceInfo = _deviceInfo with { ModelName = textMsg.Message };
                    DeviceInfoReceived?.Invoke(this, _deviceInfo);
                }
                break;
        }
    }

    #region IRadioControls Implementation (Stubbed - Not Implemented)

    /// <summary>
    /// Occurs when any radio state property changes (frequency, band, signal strength, stereo status).
    /// </summary>
    /// <remarks>
    /// TODO: Implement this event to fire when radio state changes are detected.
    /// This should integrate with the existing status update mechanism.
    /// </remarks>
    public event EventHandler<RadioStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Gets the current tuned frequency in MHz (for FM) or kHz (for AM).
    /// </summary>
    /// <remarks>
    /// TODO: Implement to return the actual current frequency from the radio device.
    /// Consider integrating with the existing CurrentStatus property.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    public double CurrentFrequency => throw new NotImplementedException("CurrentFrequency is not yet implemented. TODO: Return the current radio frequency.");

    /// <summary>
    /// Gets the current radio band (AM or FM).
    /// </summary>
    /// <remarks>
    /// TODO: Implement to return the actual current band from the radio device.
    /// Consider integrating with the existing status tracking mechanism.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    public RadioBand CurrentBand => throw new NotImplementedException("CurrentBand is not yet implemented. TODO: Return the current radio band (AM/FM).");

    /// <summary>
    /// Gets the frequency step size used for tuning up/down in MHz (FM) or kHz (AM).
    /// </summary>
    /// <remarks>
    /// TODO: Implement to return the configured frequency step size.
    /// Default values are typically 0.1 MHz for FM and 9/10 kHz for AM.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    public double FrequencyStep => throw new NotImplementedException("FrequencyStep is not yet implemented. TODO: Return the frequency step size.");

    /// <summary>
    /// Gets the current signal strength as a percentage (0-100).
    /// </summary>
    /// <remarks>
    /// TODO: Implement to return the actual signal strength from the radio device.
    /// This may require periodic polling or status updates from the device.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    public int SignalStrength => throw new NotImplementedException("SignalStrength is not yet implemented. TODO: Return the signal strength percentage.");

    /// <summary>
    /// Gets a value indicating whether the radio is receiving a stereo signal (FM only).
    /// </summary>
    /// <remarks>
    /// TODO: Implement to return the actual stereo status from the radio device.
    /// This is typically only applicable to FM band reception.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    public bool IsStereo => throw new NotImplementedException("IsStereo is not yet implemented. TODO: Return whether the signal is stereo.");

    /// <summary>
    /// Gets the current equalizer mode applied to the radio device.
    /// </summary>
    /// <remarks>
    /// TODO: Implement to return the current equalizer mode setting.
    /// This may need to be stored locally and synced with device state.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    public RadioEqualizerMode EqualizerMode => throw new NotImplementedException("EqualizerMode is not yet implemented. TODO: Return the current equalizer mode.");

    /// <summary>
    /// Gets or sets the device-specific volume level (0-100).
    /// </summary>
    /// <remarks>
    /// TODO: Implement to get/set the radio device volume.
    /// This should interact with the existing volume control commands.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    public int DeviceVolume
    {
        get => throw new NotImplementedException("DeviceVolume getter is not yet implemented. TODO: Return the current device volume level.");
        set => throw new NotImplementedException("DeviceVolume setter is not yet implemented. TODO: Set the device volume level.");
    }

    /// <summary>
    /// Gets a value indicating whether the radio is currently scanning for stations.
    /// </summary>
    /// <remarks>
    /// TODO: Implement to track scanning state.
    /// This should be updated when StartScanAsync/StopScanAsync are called.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    public bool IsScanning => throw new NotImplementedException("IsScanning is not yet implemented. TODO: Return whether the radio is scanning.");

    /// <summary>
    /// Gets the current scan direction if scanning is active; otherwise, null.
    /// </summary>
    /// <remarks>
    /// TODO: Implement to return the current scan direction when scanning is active.
    /// Should return null when not scanning.
    /// Note: This property uses explicit interface implementation because the property name
    /// conflicts with the <see cref="Radio.Core.Models.Audio.ScanDirection"/> type name.
    /// </remarks>
    /// <exception cref="NotImplementedException">This property is not yet implemented.</exception>
    ScanDirection? IRadioControls.ScanDirection => throw new NotImplementedException("ScanDirection is not yet implemented. TODO: Return the current scan direction or null.");

    /// <summary>
    /// Sets the radio frequency to a specific value.
    /// </summary>
    /// <param name="frequency">The frequency to tune to in MHz (FM) or kHz (AM).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// TODO: Implement to send the appropriate command to tune to the specified frequency.
    /// Should validate frequency is within band limits before sending.
    /// </remarks>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public Task SetFrequencyAsync(double frequency, CancellationToken ct = default)
    {
        throw new NotImplementedException("SetFrequencyAsync is not yet implemented. TODO: Implement frequency tuning command.");
    }

    /// <summary>
    /// Steps the radio frequency up by one frequency step.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// TODO: Implement to send the frequency step up command.
    /// Consider using existing NavigateAsync method as a starting point.
    /// </remarks>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public Task StepFrequencyUpAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException("StepFrequencyUpAsync is not yet implemented. TODO: Implement frequency step up command.");
    }

    /// <summary>
    /// Steps the radio frequency down by one frequency step.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// TODO: Implement to send the frequency step down command.
    /// Consider using existing NavigateAsync method as a starting point.
    /// </remarks>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public Task StepFrequencyDownAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException("StepFrequencyDownAsync is not yet implemented. TODO: Implement frequency step down command.");
    }

    /// <summary>
    /// Sets the radio band (AM or FM).
    /// </summary>
    /// <param name="band">The band to switch to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// TODO: Implement to send the band switch command.
    /// Should update internal state tracking after successful switch.
    /// </remarks>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public Task SetBandAsync(RadioBand band, CancellationToken ct = default)
    {
        throw new NotImplementedException("SetBandAsync is not yet implemented. TODO: Implement band switching command.");
    }

    /// <summary>
    /// Sets the frequency step size for tuning up/down.
    /// </summary>
    /// <param name="step">The step size in MHz (FM) or kHz (AM).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// TODO: Implement to configure the frequency step size.
    /// Should validate step size is appropriate for current band.
    /// </remarks>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public Task SetFrequencyStepAsync(double step, CancellationToken ct = default)
    {
        throw new NotImplementedException("SetFrequencyStepAsync is not yet implemented. TODO: Implement frequency step configuration.");
    }

    /// <summary>
    /// Sets the equalizer mode for the radio device.
    /// </summary>
    /// <param name="mode">The equalizer mode to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// TODO: Implement to send the equalizer mode command.
    /// Should update internal state tracking after successful change.
    /// </remarks>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public Task SetEqualizerModeAsync(RadioEqualizerMode mode, CancellationToken ct = default)
    {
        throw new NotImplementedException("SetEqualizerModeAsync is not yet implemented. TODO: Implement equalizer mode command.");
    }

    /// <summary>
    /// Starts scanning for stations in the specified direction.
    /// </summary>
    /// <param name="direction">The direction to scan (up or down).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// TODO: Implement to start frequency scanning.
    /// Should update IsScanning and ScanDirection properties.
    /// Consider using long press navigation commands as a starting point.
    /// </remarks>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public Task StartScanAsync(ScanDirection direction, CancellationToken ct = default)
    {
        throw new NotImplementedException("StartScanAsync is not yet implemented. TODO: Implement station scanning command.");
    }

    /// <summary>
    /// Stops the current scanning operation.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// TODO: Implement to stop frequency scanning.
    /// Should update IsScanning and ScanDirection properties.
    /// </remarks>
    /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
    public Task StopScanAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException("StopScanAsync is not yet implemented. TODO: Implement stop scanning command.");
    }

    /// <summary>
    /// Raises the StateChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="oldValue">The previous value.</param>
    /// <param name="newValue">The new value.</param>
    /// <remarks>
    /// This helper method should be called when any IRadioControls property changes.
    /// </remarks>
    protected virtual void OnStateChanged(string propertyName, object? oldValue = null, object? newValue = null)
    {
        StateChanged?.Invoke(this, new RadioStateChangedEventArgs(propertyName, oldValue, newValue));
    }

    #endregion

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