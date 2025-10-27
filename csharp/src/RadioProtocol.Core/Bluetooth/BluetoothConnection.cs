using RadioProtocol.Core.Logging;
using RadioProtocol.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RadioProtocol.Core.Bluetooth;

/// <summary>
/// Represents information about a discovered Bluetooth device.
/// </summary>
/// <param name="Name">The name of the device.</param>
/// <param name="Address">The address of the device.</param>
public record DeviceInfo(string Name, string Address);

/// <summary>
/// Cross-platform Bluetooth connection interface
/// </summary>
public interface IBluetoothConnection : IDisposable
{
    event EventHandler<ConnectionInfo>? ConnectionStateChanged;
    event EventHandler<byte[]>? DataReceived;

    Task<IEnumerable<DeviceInfo>> ScanForDevicesAsync(CancellationToken cancellationToken = default);
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
#if WINDOWS
        return new WindowsBluetoothConnection(logger);
#else
        if (OperatingSystem.IsLinux())
        {
            return new LinuxBluetoothConnection(logger);
        }
        else
        {
            throw new PlatformNotSupportedException($"Platform not supported: {Environment.OSVersion.Platform}");
        }
#endif
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

    public abstract Task<IEnumerable<DeviceInfo>> ScanForDevicesAsync(CancellationToken cancellationToken = default);
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