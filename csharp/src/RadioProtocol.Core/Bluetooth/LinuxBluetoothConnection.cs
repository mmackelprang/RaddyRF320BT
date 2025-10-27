#if !WINDOWS
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;
using Tmds.DBus;
#endif
using RadioProtocol.Core.Logging;
using RadioProtocol.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConnectionInfo = RadioProtocol.Core.Models.ConnectionInfo;
using DotNetConnectionState = RadioProtocol.Core.Constants.ConnectionState;

namespace RadioProtocol.Core.Bluetooth;

#if !WINDOWS
public class LinuxBluetoothConnection : BluetoothConnectionBase
{
    private static readonly Guid WriteCharacteristicUuid = new("0000ff13-0000-1000-8000-00805f9b34fb");
    private static readonly Guid NotifyCharacteristicUuid = new("0000ff14-0000-1000-8000-00805f9b34fb");

    private ConnectionInfo _connectionStatus;
    private IAdapter1? _adapter;
    private Device? _device;
    private IGattService1? _service;
    private IGattCharacteristic1? _writeCharacteristic;
    private IGattCharacteristic1? _notifyCharacteristic;
    private IDisposable? _propertyWatcher;

    public override bool IsConnected => _isConnected;
    public override ConnectionInfo ConnectionStatus => _connectionStatus;

    public LinuxBluetoothConnection(IRadioLogger logger) : base(logger)
    {
        _connectionStatus = new ConnectionInfo
        {
            State = DotNetConnectionState.Disconnected,
            Timestamp = DateTime.Now
        };
    }

    public override async Task<IEnumerable<DeviceInfo>> ScanForDevicesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInfo("Scanning for Bluetooth LE devices on Linux...");
        var devices = new List<DeviceInfo>();

        try
        {
            var adapters = await BlueZManager.GetAdaptersAsync();
            if (adapters.Count == 0)
            {
                throw new Exception("No Bluetooth adapters found.");
            }
            _adapter = adapters.First();

            await _adapter.StartDiscoveryAsync();
            await Task.Delay(5000, cancellationToken); // Scan for 5 seconds
            await _adapter.StopDiscoveryAsync();

            var discoveredDevices = await _adapter.GetDevicesAsync();
            foreach (var d in discoveredDevices)
            {
                var name = await d.GetNameAsync();
                if (!string.IsNullOrEmpty(name))
                {
                    var address = await d.GetAddressAsync();
                    devices.Add(new DeviceInfo(name, address));
                    _logger.LogInfo($"Found device: {name} ({address})");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan for devices on Linux.");
        }

        return devices;
    }

    public override async Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInfo($"Attempting to connect to Linux Bluetooth device: {deviceAddress}");
            OnConnectionStateChanged(_connectionStatus with { State = DotNetConnectionState.Connecting, DeviceAddress = deviceAddress, Timestamp = DateTime.Now });

            if (_adapter == null)
            {
                var adapters = await BlueZManager.GetAdaptersAsync();
                if (adapters.Count == 0) throw new Exception("No Bluetooth adapters found.");
                _adapter = adapters.First();
            }

            _device = await _adapter.GetDeviceAsync(deviceAddress);
            if (_device == null)
            {
                throw new Exception("Device not found.");
            }

            await _device.ConnectAsync();
            await _device.WaitForPropertyValueAsync("Connected", value: true, TimeSpan.FromSeconds(15));
            _logger.LogInfo("Device connected.");

            await _device.WaitForPropertyValueAsync("ServicesResolved", value: true, TimeSpan.FromSeconds(15));
            _logger.LogInfo("Services resolved.");

            var services = await _device.GetServicesAsync();
            foreach (var service in services)
            {
                var characteristics = await service.GetCharacteristicsAsync();
                if (characteristics.Any(c => c.GetUUIDAsync().Result == NotifyCharacteristicUuid.ToString()))
                {
                    _service = service;
                    break;
                }
            }


            if (_service == null)
            {
                throw new Exception("Required service not found.");
            }
            _logger.LogInfo($"Service found: {await _service.GetUUIDAsync()}");

            _writeCharacteristic = await _service.GetCharacteristicAsync(WriteCharacteristicUuid.ToString());
            _notifyCharacteristic = await _service.GetCharacteristicAsync(NotifyCharacteristicUuid.ToString());

            if (_writeCharacteristic == null || _notifyCharacteristic == null)
            {
                throw new Exception("Required characteristics not found.");
            }

            _propertyWatcher = await _notifyCharacteristic.WatchPropertiesAsync(OnCharacteristicValueChanged);
            await _notifyCharacteristic.StartNotifyAsync();

            _isConnected = true;
            OnConnectionStateChanged(_connectionStatus with { State = DotNetConnectionState.Connected, Timestamp = DateTime.Now });
            _logger.LogInfo("Linux Bluetooth connection established.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Linux Bluetooth device.");
            OnConnectionStateChanged(_connectionStatus with { State = DotNetConnectionState.Error, ErrorMessage = ex.Message, Timestamp = DateTime.Now });
            return false;
        }
    }

    private void OnCharacteristicValueChanged(PropertyChanges changes)
    {
        foreach (var change in changes.Changed)
        {
            if (change.Key == "Value")
            {
                if (change.Value is byte[] bytes)
                {
                    OnDataReceived(bytes);
                }
            }
        }
    }

    public override async Task DisconnectAsync()
    {
        _logger.LogInfo("Disconnecting Linux Bluetooth device.");
        _propertyWatcher?.Dispose();

        if (_device != null)
        {
            if (await _device.GetConnectedAsync())
            {
                if (_notifyCharacteristic != null && await _notifyCharacteristic.GetNotifyingAsync())
                {
                    await _notifyCharacteristic.StopNotifyAsync();
                }
                await _device.DisconnectAsync();
            }
        }
        _isConnected = false;
        OnConnectionStateChanged(_connectionStatus with { State = DotNetConnectionState.Disconnected, Timestamp = DateTime.Now });
        _logger.LogInfo("Linux Bluetooth disconnected.");
    }

    public override async Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _writeCharacteristic == null)
        {
            _logger.LogWarning("Cannot send data - not connected or write characteristic not available.");
            return false;
        }

        try
        {
            await _writeCharacteristic.WriteValueAsync(data, new Dictionary<string, object> { { "type", "command" } });
            _logger.LogRawDataSent(data);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data via Linux Bluetooth.");
            return false;
        }
    }
}
#endif
