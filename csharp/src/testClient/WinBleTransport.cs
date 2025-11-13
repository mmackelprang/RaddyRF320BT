using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace RadioClient;

public sealed class WinBleTransport : IRadioTransport
{
    private readonly BluetoothLEDevice _device;
    private readonly MessageLogger? _logger;
    private GattCharacteristic? _txCharacteristic;      // ff13 - documented TX
    private GattCharacteristic? _txAlternative;         // fff1 - might be real TX
    private GattCharacteristic? _rxCharacteristic;
    private GattCharacteristic? _rxAckCharacteristic;  // fff1 - for ACK messages
    private bool _isDisposed;

    public event EventHandler<byte[]>? NotificationReceived;

    // Service and Characteristic UUIDs for RF320 device
    // Based on actual device discovery: Service ff10 has fff1, Service ff12 has ff13/ff14/ff15
    private static readonly Guid RxServiceUuid = Guid.Parse("0000ff10-0000-1000-8000-00805f9b34fb");
    private static readonly Guid TxServiceUuid = Guid.Parse("0000ff12-0000-1000-8000-00805f9b34fb");
    private static readonly Guid TxCharacteristicUuid = Guid.Parse("0000ff13-0000-1000-8000-00805f9b34fb"); // Write (to device)
    private static readonly Guid RxAckCharacteristicUuid = Guid.Parse("0000fff1-0000-1000-8000-00805f9b34fb"); // Notify ACK (from device)
    private static readonly Guid RxStatusCharacteristicUuid = Guid.Parse("0000ff14-0000-1000-8000-00805f9b34fb"); // Notify Status (from device)

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
            
            // RX Status characteristic: ff14 (from TX service ff12) - for Status messages (Group 0x1C)
            _rxCharacteristic = txCharsResult.Characteristics.FirstOrDefault(c => c.Uuid == RxStatusCharacteristicUuid);
            
            // RX ACK characteristic: fff1 (from RX service ff10) - for ACK messages (Group 0x12)
            _rxAckCharacteristic = rxCharsResult.Characteristics.FirstOrDefault(c => c.Uuid == RxAckCharacteristicUuid);

            if (_txCharacteristic == null)
            {
                Log($"TX characteristic {TxCharacteristicUuid} not found");
                return false;
            }

            if (_rxCharacteristic == null && _rxAckCharacteristic == null)
            {
                Log($"No RX characteristics found (Status {RxStatusCharacteristicUuid}, ACK {RxAckCharacteristicUuid})");
                return false;
            }

            Log("Characteristics found, enabling notifications...");

            // Enable notifications on Status characteristic (ff14) if available
            if (_rxCharacteristic != null)
            {
                Log($"Found Status RX characteristic {_rxCharacteristic.Uuid} (properties: {_rxCharacteristic.CharacteristicProperties})");
                _rxCharacteristic.ValueChanged += OnCharacteristicValueChanged;

                try
                {
                    var cccdResult = await _rxCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.Notify);

                    if (cccdResult != GattCommunicationStatus.Success)
                    {
                        Log($"Warning: Failed to enable Status notifications via CCCD: {cccdResult}");
                    }
                    else
                    {
                        Log("Status notifications (ff14) enabled successfully");
                    }
                }
                catch (Exception cccdEx)
                {
                    Log($"Warning: Exception enabling Status notifications: {cccdEx.Message} (HResult: 0x{cccdEx.HResult:X8})");
                }
            }

            // Try fff1 characteristic - might be alternative TX (Write) channel instead of RX
            if (_rxAckCharacteristic != null)
            {
                Log($"Found fff1 characteristic {_rxAckCharacteristic.Uuid} (properties: {_rxAckCharacteristic.CharacteristicProperties})");
                
                // Check if this is actually a WRITE characteristic (not notify)
                if (_rxAckCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write) ||
                    _rxAckCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                {
                    Log("fff1 has Write property - storing as alternative TX channel");
                    _txAlternative = _rxAckCharacteristic;
                    // We'll try sending commands to BOTH ff13 and fff1 to see which works
                }
                else if (_rxAckCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
                {
                    // Try to enable notifications
                    Log("fff1 has Notify property - attempting to subscribe...");
                    _rxAckCharacteristic.ValueChanged += OnCharacteristicValueChanged;

                    bool ackNotifySuccess = false;
                    try
                    {
                        var cccdResult = await _rxAckCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                            GattClientCharacteristicConfigurationDescriptorValue.Notify);

                        if (cccdResult != GattCommunicationStatus.Success)
                        {
                            Log($"Failed to enable fff1 notifications: {cccdResult}");
                        }
                        else
                        {
                            Log("fff1 notifications enabled successfully");
                            ackNotifySuccess = true;
                        }
                    }
                    catch (Exception cccdEx)
                    {
                        // HResult 0x80650003 = E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED
                        if (cccdEx.HResult == unchecked((int)0x80650003))
                        {
                            Log("fff1 notifications require pairing - attempting to pair device...");
                            
                            if (await TryPairDeviceAsync())
                            {
                                Log("Pairing successful, retrying fff1 notification subscription...");
                                
                                try
                                {
                                    var retryResult = await _rxAckCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                        GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                    
                                    if (retryResult == GattCommunicationStatus.Success)
                                    {
                                        Log("fff1 notifications enabled successfully after pairing");
                                        ackNotifySuccess = true;
                                    }
                                    else
                                    {
                                        Log($"Failed to enable fff1 notifications after pairing: {retryResult}");
                                    }
                                }
                                catch (Exception retryEx)
                                {
                                    Log($"Exception enabling fff1 notifications after pairing: {retryEx.Message}");
                                }
                            }
                            else
                            {
                                Log("Pairing failed - fff1 notifications not available");
                            }
                        }
                        else
                        {
                            Log($"Exception enabling fff1 notifications: {cccdEx.Message} (HResult: 0x{cccdEx.HResult:X8})");
                        }
                    }
                    
                    if (!ackNotifySuccess)
                    {
                        Log("Note: fff1 notifications not enabled, relying on ff14 for all responses");
                    }
                }
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

    private async Task<bool> TryPairDeviceAsync()
    {
        try
        {
            Log($"Requesting pairing for device {_device.DeviceId}...");
            
            // Get the DeviceInformation for pairing
            var deviceInfo = await DeviceInformation.CreateFromIdAsync(_device.DeviceId);
            
            if (deviceInfo == null)
            {
                Log("Failed to get device information for pairing");
                return false;
            }

            // Check current pairing status
            if (deviceInfo.Pairing.IsPaired)
            {
                Log("Device is already paired");
                return true;
            }

            // Attempt to pair
            Log("Device not paired, initiating pairing...");
            var pairingResult = await deviceInfo.Pairing.PairAsync();
            
            if (pairingResult.Status == DevicePairingResultStatus.Paired ||
                pairingResult.Status == DevicePairingResultStatus.AlreadyPaired)
            {
                Log($"Pairing successful: {pairingResult.Status}");
                return true;
            }
            else
            {
                Log($"Pairing failed: {pairingResult.Status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log($"Exception during pairing: {ex.Message} (HResult: 0x{ex.HResult:X8})");
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
            Log($"RX: {BitConverter.ToString(data)}", writeToConsole: false);
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

        // CRITICAL: Radio requires WriteWithResponse (BLE-level ACK) for commands to work
        try
        {
            Log($"Writing {data.Length} bytes: {BitConverter.ToString(data)}");
            var writer = new DataWriter();
            writer.WriteBytes(data);
            var result = await _txCharacteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse);
            Log($"Write result: {result}");
            return result == GattCommunicationStatus.Success;
        }
        catch (Exception ex)
        {
            Log($"Error writing data: {ex.Message}");
            return false;
        }
    }

    private void Log(string message, bool writeToConsole = true)
    {
        if (writeToConsole)
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

        if (_rxAckCharacteristic != null)
        {
            _rxAckCharacteristic.ValueChanged -= OnCharacteristicValueChanged;
        }

        _device?.Dispose();
    }
}
