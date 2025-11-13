using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace RadioClient;

public sealed class WinBleTransport : IRadioTransport
{
    private readonly BluetoothLEDevice _device;
    private readonly MessageLogger? _logger;
    private GattCharacteristic? _txCharacteristic;
    private GattCharacteristic? _rxCharacteristic;
    private bool _isDisposed;

    public event EventHandler<byte[]>? NotificationReceived;

    // Service and Characteristic UUIDs for RF320 device
    // Based on actual device discovery: Service ff10 has fff1, Service ff12 has ff13/ff14/ff15
    private static readonly Guid RxServiceUuid = Guid.Parse("0000ff10-0000-1000-8000-00805f9b34fb");
    private static readonly Guid TxServiceUuid = Guid.Parse("0000ff12-0000-1000-8000-00805f9b34fb");
    private static readonly Guid TxCharacteristicUuid = Guid.Parse("0000ff13-0000-1000-8000-00805f9b34fb"); // Write (to device)
    private static readonly Guid RxCharacteristicUuid = Guid.Parse("0000fff4-0000-1000-8000-00805f9b34fb"); // Notify (from device)
//    private static readonly Guid RxCharacteristicUuid = Guid.Parse("0000fff1-0000-1000-8000-00805f9b34fb"); // Notify (from device)
    private static readonly Guid RxAltCharacteristicUuid = Guid.Parse("0000ff14-0000-1000-8000-00805f9b34fb"); // Alternative notify

    private WinBleTransport(BluetoothLEDevice device, MessageLogger? logger = null)
    {
        _device = device;
        _logger = logger;
    }

    public static async Task<WinBleTransport?> ConnectAsync(BluetoothLEDevice device, MessageLogger? logger = null, CancellationToken ct = default)
    {
        var transport = new WinBleTransport(device, logger);
        if (await transport.InitializeAsync(ct))
        {
            return transport;
        }
        transport.Dispose();
        return null;
    }

    private async Task<bool> InitializeAsync(CancellationToken ct)
    {
        try
        {
            Log("Discovering GATT services...");
            var servicesResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            
            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                Log($"Failed to get GATT services: {servicesResult.Status}");
                return false;
            }

            Log($"Found {servicesResult.Services.Count} services:");
            foreach (var service in servicesResult.Services)
            {
                Log($"  Service: {service.Uuid}");
            }

            // Find RX service (ff10) and TX service (ff12)
            var rxService = servicesResult.Services.FirstOrDefault(s => s.Uuid == RxServiceUuid);
            var txService = servicesResult.Services.FirstOrDefault(s => s.Uuid == TxServiceUuid);

            if (rxService == null || txService == null)
            {
                Log($"Required services not found (RX: {rxService != null}, TX: {txService != null})");
                Log("Trying alternative service discovery...");
                
                // List all characteristics from all services to help debug
                foreach (var service in servicesResult.Services)
                {
                    var chars = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (chars.Status == GattCommunicationStatus.Success)
                    {
                        Log($"  Service {service.Uuid} has {chars.Characteristics.Count} characteristics:");
                        foreach (var ch in chars.Characteristics)
                        {
                            Log($"    Characteristic: {ch.Uuid} (Props: {ch.CharacteristicProperties})");
                        }
                    }
                }
                
                return false;
            }

            Log("Services found, discovering TX characteristics...");
            var txCharsResult = await txService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            
            if (txCharsResult.Status != GattCommunicationStatus.Success)
            {
                Log($"Failed to get TX characteristics: {txCharsResult.Status}");
                return false;
            }

            Log("Discovering RX characteristics...");
            var rxCharsResult = await rxService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            
            if (rxCharsResult.Status != GattCommunicationStatus.Success)
            {
                Log($"Failed to get RX characteristics: {rxCharsResult.Status}");
                return false;
            }

            // TX characteristic: ff13 (Write)
            _txCharacteristic = txCharsResult.Characteristics.FirstOrDefault(c => c.Uuid == TxCharacteristicUuid);
            
            // RX characteristic: Try ff14 first (from TX service), then fff1 (from RX service)
            _rxCharacteristic = txCharsResult.Characteristics.FirstOrDefault(c => c.Uuid == RxAltCharacteristicUuid);
            if (_rxCharacteristic != null)
            {
                Log($"Using RX characteristic from TX service: {_rxCharacteristic.Uuid}");
            }
            else
            {
                _rxCharacteristic = rxCharsResult.Characteristics.FirstOrDefault(c => c.Uuid == RxCharacteristicUuid);
                if (_rxCharacteristic != null)
                {
                    Log($"Using RX characteristic from RX service: {_rxCharacteristic.Uuid}");
                }
            }

            if (_txCharacteristic == null)
            {
                Log($"TX characteristic {TxCharacteristicUuid} not found");
                return false;
            }

            if (_rxCharacteristic == null)
            {
                Log($"RX characteristic {RxCharacteristicUuid} not found");
                return false;
            }

            Log("Characteristics found, enabling notifications...");
            Log($"RX Characteristic {_rxCharacteristic.Uuid} properties: {_rxCharacteristic.CharacteristicProperties}");

            // Subscribe to value changed event first
            _rxCharacteristic.ValueChanged += OnCharacteristicValueChanged;

            // Enable notifications on RX characteristic
            try
            {
                var cccdResult = await _rxCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);

                if (cccdResult != GattCommunicationStatus.Success)
                {
                    Log($"Warning: Failed to enable notifications via CCCD: {cccdResult}");
                    Log("Attempting to continue anyway - notifications may still work if device auto-enables");
                }
                else
                {
                    Log("Notifications enabled successfully via CCCD");
                }
            }
            catch (Exception cccdEx)
            {
                Log($"Warning: Exception enabling notifications: {cccdEx.Message} (HResult: 0x{cccdEx.HResult:X8})");
                Log("Attempting to continue anyway - notifications may still work if device auto-enables");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log($"Error initializing transport: {ex.GetType().Name}");
            Log($"Message: '{ex.Message}'");
            Log($"HResult: 0x{ex.HResult:X8}");
            if (ex.InnerException != null)
            {
                Log($"Inner: {ex.InnerException.Message}");
            }
            Log($"Stack: {ex.StackTrace}");
            return false;
        }
    }

    private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        try
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            var data = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(data);
            Log($"Received notification: {BitConverter.ToString(data)}");
            NotificationReceived?.Invoke(this, data);
        }
        catch (Exception ex)
        {
            Log($"Error handling notification: {ex.Message}");
        }
    }

    public async Task<bool> WriteAsync(byte[] data)
    {
        if (_isDisposed || _txCharacteristic == null)
        {
            Log("Write failed: Transport disposed or TX characteristic null");
            return false;
        }

        try
        {
            Log($"Writing {data.Length} bytes: {BitConverter.ToString(data)}");
            var writer = new DataWriter();
            writer.WriteBytes(data);
            var result = await _txCharacteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
            Log($"Write result: {result}");
            return result == GattCommunicationStatus.Success;
        }
        catch (Exception ex)
        {
            Log($"Error writing data: {ex.Message}");
            return false;
        }
    }

    private void Log(string message)
    {
        Console.WriteLine($"  [BLE] {message}");
        _logger?.LogInfo($"BLE: {message}");
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_rxCharacteristic != null)
        {
            _rxCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
        }

        _device?.Dispose();
    }
}
