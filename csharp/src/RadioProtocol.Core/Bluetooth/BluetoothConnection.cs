using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Models;
using RadioProtocol.Core.Logging;

namespace RadioProtocol.Core.Bluetooth;

/// <summary>
/// Cross-platform Bluetooth connection interface
/// </summary>
public interface IBluetoothConnection : IDisposable
{
    event EventHandler<ConnectionInfo>? ConnectionStateChanged;
    event EventHandler<byte[]>? DataReceived;
    
    Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default);
    bool IsConnected { get; }
    ConnectionInfo ConnectionStatus { get; }
}

/// <summary>
/// Bluetooth connection factory for cross-platform support
/// </summary>
public static class BluetoothConnectionFactory
{
    public static IBluetoothConnection Create(IRadioLogger logger)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsBluetoothConnection(logger);
        }
        else if (OperatingSystem.IsLinux())
        {
            return new LinuxBluetoothConnection(logger);
        }
        else
        {
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");
        }
    }
}

/// <summary>
/// Base Bluetooth connection implementation
/// </summary>
public abstract class BluetoothConnectionBase : IBluetoothConnection
{
    protected readonly IRadioLogger _logger;
    protected volatile bool _isConnected;
    protected volatile bool _disposed;
    
    public event EventHandler<ConnectionInfo>? ConnectionStateChanged;
    public event EventHandler<byte[]>? DataReceived;
    
    public abstract bool IsConnected { get; }
    public abstract ConnectionInfo ConnectionStatus { get; }

    protected BluetoothConnectionBase(IRadioLogger logger)
    {
        _logger = logger;
    }

    public abstract Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default);
    public abstract Task DisconnectAsync();
    public abstract Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default);

    protected virtual void OnConnectionStateChanged(ConnectionInfo connectionInfo)
    {
        _logger.LogInfo($"Connection state changed: {connectionInfo.State}");
        ConnectionStateChanged?.Invoke(this, connectionInfo);
    }

    protected virtual void OnDataReceived(byte[] data)
    {
        _logger.LogRawDataReceived(data);
        DataReceived?.Invoke(this, data);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Task.Run(async () => await DisconnectAsync()).Wait(5000);
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
/// Windows-specific Bluetooth LE implementation
/// </summary>
public class WindowsBluetoothConnection : BluetoothConnectionBase
{
    // Placeholder GUIDs - these would need to be actual radio device UUIDs
    private static readonly Guid SERVICE_UUID = new("0000ff12-0000-1000-8000-00805f9b34fb");
    private static readonly Guid WRITE_CHARACTERISTIC_UUID = new("0000ff13-0000-1000-8000-00805f9b34fb");
    private static readonly Guid NOTIFY_CHARACTERISTIC_UUID = new("0000ff14-0000-1000-8000-00805f9b34fb");

    private ConnectionInfo _connectionStatus;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public override bool IsConnected => _isConnected;
    public override ConnectionInfo ConnectionStatus => _connectionStatus;

    public WindowsBluetoothConnection(IRadioLogger logger) : base(logger)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _connectionStatus = new ConnectionInfo
        {
            State = ConnectionState.Disconnected,
            Timestamp = DateTime.Now
        };
    }

    public override async Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInfo($"Attempting to connect to Windows Bluetooth device: {deviceAddress}");
            
            _connectionStatus = _connectionStatus with 
            { 
                State = ConnectionState.Connecting,
                DeviceAddress = deviceAddress,
                Timestamp = DateTime.Now
            };
            OnConnectionStateChanged(_connectionStatus);

            // Windows Bluetooth LE implementation would go here
            // For now, simulate connection for demonstration
            await Task.Delay(1000, cancellationToken);

            _isConnected = true;
            _connectionStatus = _connectionStatus with 
            { 
                State = ConnectionState.Connected,
                Timestamp = DateTime.Now
            };
            OnConnectionStateChanged(_connectionStatus);

            _logger.LogInfo("Windows Bluetooth connection established");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Windows Bluetooth device");
            _connectionStatus = _connectionStatus with 
            { 
                State = ConnectionState.Error,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.Now
            };
            OnConnectionStateChanged(_connectionStatus);
            return false;
        }
    }

    public override async Task DisconnectAsync()
    {
        try
        {
            _logger.LogInfo("Disconnecting Windows Bluetooth device");
            
            _isConnected = false;
            _connectionStatus = _connectionStatus with 
            { 
                State = ConnectionState.Disconnected,
                Timestamp = DateTime.Now
            };
            OnConnectionStateChanged(_connectionStatus);

            await Task.CompletedTask;
            _logger.LogInfo("Windows Bluetooth disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Windows Bluetooth disconnection");
        }
    }

    public override async Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            _logger.LogWarning("Cannot send data - not connected");
            return false;
        }

        try
        {
            _logger.LogRawDataSent(data);
            // Windows Bluetooth LE write implementation would go here
            await Task.Delay(10, cancellationToken); // Simulate transmission time
            
            _logger.LogDebug($"Sent {data.Length} bytes via Windows Bluetooth");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data via Windows Bluetooth");
            return false;
        }
    }
}

/// <summary>
/// Linux/Raspberry Pi Bluetooth implementation
/// </summary>
public class LinuxBluetoothConnection : BluetoothConnectionBase
{
    private ConnectionInfo _connectionStatus;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public override bool IsConnected => _isConnected;
    public override ConnectionInfo ConnectionStatus => _connectionStatus;

    public LinuxBluetoothConnection(IRadioLogger logger) : base(logger)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _connectionStatus = new ConnectionInfo
        {
            State = ConnectionState.Disconnected,
            Timestamp = DateTime.Now
        };
    }

    public override async Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInfo($"Attempting to connect to Linux Bluetooth device: {deviceAddress}");
            
            _connectionStatus = _connectionStatus with 
            { 
                State = ConnectionState.Connecting,
                DeviceAddress = deviceAddress,
                Timestamp = DateTime.Now
            };
            OnConnectionStateChanged(_connectionStatus);

            // Linux BlueZ implementation would go here
            // This could use System.Diagnostics.Process to call bluetoothctl/gatttool
            // Or use a library like dotnet-bluetooth
            await Task.Delay(1000, cancellationToken);

            _isConnected = true;
            _connectionStatus = _connectionStatus with 
            { 
                State = ConnectionState.Connected,
                Timestamp = DateTime.Now
            };
            OnConnectionStateChanged(_connectionStatus);

            _logger.LogInfo("Linux Bluetooth connection established");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Linux Bluetooth device");
            _connectionStatus = _connectionStatus with 
            { 
                State = ConnectionState.Error,
                ErrorMessage = ex.Message,
                Timestamp = DateTime.Now
            };
            OnConnectionStateChanged(_connectionStatus);
            return false;
        }
    }

    public override async Task DisconnectAsync()
    {
        try
        {
            _logger.LogInfo("Disconnecting Linux Bluetooth device");
            
            _isConnected = false;
            _connectionStatus = _connectionStatus with 
            { 
                State = ConnectionState.Disconnected,
                Timestamp = DateTime.Now
            };
            OnConnectionStateChanged(_connectionStatus);

            await Task.CompletedTask;
            _logger.LogInfo("Linux Bluetooth disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Linux Bluetooth disconnection");
        }
    }

    public override async Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            _logger.LogWarning("Cannot send data - not connected");
            return false;
        }

        try
        {
            _logger.LogRawDataSent(data);
            // Linux Bluetooth implementation would go here
            await Task.Delay(10, cancellationToken); // Simulate transmission time
            
            _logger.LogDebug($"Sent {data.Length} bytes via Linux Bluetooth");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data via Linux Bluetooth");
            return false;
        }
    }
}