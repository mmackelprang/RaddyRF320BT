#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Logging;
using RadioProtocol.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadioProtocol.Core.Bluetooth;

/// <summary>
/// Windows-specific Bluetooth LE implementation
/// </summary>
public class WindowsBluetoothConnection : BluetoothConnectionBase
{
    private static readonly Guid WriteCharacteristicUuid = new("0000ff13-0000-1000-8000-00805f9b34fb");
    private static readonly Guid NotifyCharacteristicUuid = new("0000ff14-0000-1000-8000-00805f9b34fb");

    private BluetoothLEAdvertisementWatcher? _watcher;
    private readonly List<DeviceInfo> _discoveredDevices = new();
    private ConnectionInfo _connectionStatus;
    private BluetoothLEDevice? _device;
    private GattDeviceService? _service;
    private GattCharacteristic? _writeCharacteristic;
    private GattCharacteristic? _notifyCharacteristic;

    public override bool IsConnected => _isConnected;
    public override ConnectionInfo ConnectionStatus => _connectionStatus;

    public WindowsBluetoothConnection(IRadioLogger logger) : base(logger)
    {
        _connectionStatus = new ConnectionInfo
        {
            State = ConnectionState.Disconnected,
            Timestamp = DateTime.Now
        };
    }

    public override async Task<IEnumerable<DeviceInfo>> ScanForDevicesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInfo("Scanning for Bluetooth LE devices on Windows...");
        _discoveredDevices.Clear();

        _watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        var tcs = new TaskCompletionSource<bool>();
        cancellationToken.Register(() => tcs.TrySetCanceled());

        _watcher.Received += OnAdvertisementReceived;
        _watcher.Start();

        // Watch for 5 seconds
        await Task.WhenAny(Task.Delay(5000, cancellationToken), tcs.Task);

        _watcher.Stop();
        _watcher.Received -= OnAdvertisementReceived;
        _watcher = null;

        _logger.LogInfo($"Scan complete. Found {_discoveredDevices.Count} devices.");
        return _discoveredDevices;
    }

    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        if (!string.IsNullOrEmpty(args.Advertisement.LocalName) && !_discoveredDevices.Any(d => d.Address == args.BluetoothAddress.ToString()))
        {
            var deviceInfo = new DeviceInfo(args.Advertisement.LocalName, args.BluetoothAddress.ToString());
            _discoveredDevices.Add(deviceInfo);
            _logger.LogInfo($"Found device: {deviceInfo.Name} ({deviceInfo.Address})");
        }
    }

    public override async Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInfo($"Attempting to connect to Windows Bluetooth device: {deviceAddress}");
            OnConnectionStateChanged(_connectionStatus with { State = ConnectionState.Connecting, DeviceAddress = deviceAddress, Timestamp = DateTime.Now });

            var bluetoothAddress = ulong.Parse(deviceAddress);
            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);

            if (_device == null)
            {
                throw new Exception("Device not found.");
            }

            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            var gattResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (gattResult.Status != GattCommunicationStatus.Success)
            {
                throw new Exception($"Failed to get GATT services: {gattResult.Status}");
            }

            var services = gattResult.Services;
            _logger.LogInfo($"Found {services.Count} services.");

            // The service UUID is not fixed, find it by looking for the write characteristic
            foreach (var service in services)
            {
                var charResult = await service.GetCharacteristicsForUuidAsync(WriteCharacteristicUuid, BluetoothCacheMode.Uncached);
                if (charResult.Status == GattCommunicationStatus.Success && charResult.Characteristics.Any())
                {
                    _service = service;
                    break;
                }
            }

            if (_service == null)
            {
                throw new Exception("Required service not found.");
            }
            _logger.LogInfo($"Service found: {_service.Uuid}");

            var writeCharResult = await _service.GetCharacteristicsForUuidAsync(WriteCharacteristicUuid);
            if (writeCharResult.Status != GattCommunicationStatus.Success) throw new Exception("Write characteristic not found.");
            _writeCharacteristic = writeCharResult.Characteristics.First();

            var notifyCharResult = await _service.GetCharacteristicsForUuidAsync(NotifyCharacteristicUuid);
            if (notifyCharResult.Status != GattCommunicationStatus.Success) throw new Exception("Notify characteristic not found.");
            _notifyCharacteristic = notifyCharResult.Characteristics.First();

            _notifyCharacteristic.ValueChanged += OnCharacteristicValueChanged;
            var status = await _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status != GattCommunicationStatus.Success)
            {
                throw new Exception($"Failed to start notifications: {status}");
            }

            _isConnected = true;
            OnConnectionStateChanged(_connectionStatus with { State = ConnectionState.Connected, Timestamp = DateTime.Now });
            _logger.LogInfo("Windows Bluetooth connection established.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Windows Bluetooth device.");
            OnConnectionStateChanged(_connectionStatus with { State = ConnectionState.Error, ErrorMessage = ex.Message, Timestamp = DateTime.Now });
            return false;
        }
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
        {
            _isConnected = false;
            OnConnectionStateChanged(_connectionStatus with { State = ConnectionState.Disconnected, Timestamp = DateTime.Now });
            _logger.LogInfo("Windows Bluetooth device disconnected.");
        }
    }

    private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        OnDataReceived(args.CharacteristicValue.ToArray());
    }

    public override async Task DisconnectAsync()
    {
        _logger.LogInfo("Disconnecting Windows Bluetooth device.");
        if (_device != null)
        {
            _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        if (_notifyCharacteristic != null)
        {
            try
            {
                await _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                _notifyCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping notifications.");
            }
        }

        _service?.Dispose();
        _device?.Dispose();
        _service = null;
        _device = null;
        _writeCharacteristic = null;
        _notifyCharacteristic = null;
        _isConnected = false;

        OnConnectionStateChanged(_connectionStatus with { State = ConnectionState.Disconnected, Timestamp = DateTime.Now });
        _logger.LogInfo("Windows Bluetooth disconnected.");
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
            var status = await _writeCharacteristic.WriteValueAsync(data.AsBuffer(), GattWriteOption.WriteWithoutResponse);
            if (status == GattCommunicationStatus.Success)
            {
                _logger.LogRawDataSent(data);
                return true;
            }
            _logger.LogWarning($"Failed to send data, status: {status}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data via Windows Bluetooth.");
            return false;
        }
    }
}
#endif
