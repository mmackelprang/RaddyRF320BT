using BtMock.Config;
using Microsoft.Extensions.Logging;
#if WINDOWS
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace BtMock.Bluetooth;

/// <summary>
/// Implements a Bluetooth LE peripheral that acts as a mock radio device.
/// This class handles advertising, connection management, and GATT service/characteristic operations.
/// 
/// Note: This implementation uses a combination of approaches:
/// 1. Windows.Devices.Bluetooth (UWP) for Windows-native support
/// 2. InTheHand.BluetoothLE as a cross-platform fallback
/// 
/// The actual Bluetooth operations are async to ensure responsiveness.
/// </summary>
public class BluetoothPeripheral
{
    private readonly BluetoothConfiguration _config;
    private readonly ILogger _logger;
    private bool _isAdvertising;
    private bool _isConnected;
    
    // Simulated connection state
    private readonly List<byte[]> _notificationQueue = new();
    private readonly object _lock = new();

#if WINDOWS
    private GattServiceProvider? _serviceProvider;
    private GattLocalCharacteristic? _writeCharacteristic;
    private GattLocalCharacteristic? _notifyCharacteristic;
    private IReadOnlyList<GattSubscribedClient>? _subscribedClients;
#endif

    /// <summary>
    /// Event raised when a message is received from a connected controller.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Event raised when the connection status changes.
    /// </summary>
    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    /// <summary>
    /// Gets whether the peripheral is currently connected to a controller.
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Initializes a new Bluetooth peripheral with the specified configuration.
    /// </summary>
    /// <param name="config">Bluetooth configuration settings</param>
    /// <param name="logger">Logger for diagnostic messages</param>
    public BluetoothPeripheral(BluetoothConfiguration config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Starts advertising as a Bluetooth LE peripheral.
    /// This makes the mock radio discoverable to scanning applications.
    /// </summary>
    public async Task StartAdvertisingAsync()
    {
        try
        {
            _logger.LogInformation("Starting Bluetooth LE peripheral advertising");
            
            // Platform-specific initialization would go here
            // For now, we'll simulate the advertising state
            await InitializePlatformBluetoothAsync();
            
            _isAdvertising = true;
            OnConnectionStatusChanged("Advertising", "");
            
            _logger.LogInformation("Bluetooth peripheral is now advertising");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start advertising");
            throw;
        }
    }

    /// <summary>
    /// Initializes platform-specific Bluetooth functionality.
    /// This method handles the differences between Windows UWP and other platforms.
    /// </summary>
    private async Task InitializePlatformBluetoothAsync()
    {
        #if WINDOWS
        _logger.LogInformation("Initializing Windows Bluetooth LE peripheral");

        // 1. Create a GattServiceProvider
        var serviceUuid = Guid.Parse(_config.ServiceUUID);
        var result = await GattServiceProvider.CreateAsync(serviceUuid);

        if (result.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Could not create GATT service provider: {result.Error}");
        }
        _serviceProvider = result.ServiceProvider;
        _logger.LogInformation($"GATT service provider created for UUID: {_serviceProvider.Service.Uuid}");

        // 2. Create Write Characteristic
        var writeUuid = Guid.Parse(_config.WriteCharacteristicUUID);
        var writeParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.WriteWithoutResponse,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Radio Write Characteristic"
        };
        var charResult = await _serviceProvider.Service.CreateCharacteristicAsync(writeUuid, writeParameters);
        if (charResult.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Could not create write characteristic: {charResult.Error}");
        }
        _writeCharacteristic = charResult.Characteristic;
        _writeCharacteristic.WriteRequested += OnWriteRequested;
        _logger.LogInformation($"Write characteristic created: {_writeCharacteristic.Uuid}");

        // 3. Create Notify Characteristic
        var notifyUuid = Guid.Parse(_config.NotifyCharacteristicUUID);
        var notifyParameters = new GattLocalCharacteristicParameters
        {
            CharacteristicProperties = GattCharacteristicProperties.Notify,
            WriteProtectionLevel = GattProtectionLevel.Plain,
            UserDescription = "Radio Notify Characteristic"
        };
        charResult = await _serviceProvider.Service.CreateCharacteristicAsync(notifyUuid, notifyParameters);
        if (charResult.Error != BluetoothError.Success)
        {
            throw new InvalidOperationException($"Could not create notify characteristic: {charResult.Error}");
        }
        _notifyCharacteristic = charResult.Characteristic;
        _notifyCharacteristic.SubscribedClientsChanged += OnSubscribedClientsChanged;
        _logger.LogInformation($"Notify characteristic created: {_notifyCharacteristic.Uuid}");

        // 4. Start Advertising Service and Device Name
        var advertisement = new BluetoothLEAdvertisement();
        advertisement.LocalName = _config.DeviceName;
        advertisement.ServiceUuids.Add(serviceUuid);

        var advParameters = new GattServiceProviderAdvertisingParameters
        {
            IsDiscoverable = true,
            IsConnectable = true,
        };
        _serviceProvider.StartAdvertising(advParameters);
        _logger.LogInformation($"GATT service advertising started with device name '{_config.DeviceName}'.");

        #else
        // Non-Windows platforms can use InTheHand.BluetoothLE or other libraries
        _logger.LogInformation("Initializing cross-platform Bluetooth LE peripheral");
        
        // Note: InTheHand.BluetoothLE provides cross-platform Bluetooth LE support
        // However, peripheral/server mode support varies by platform
        
        await Task.Delay(500); // Simulated initialization delay
        _logger.LogInformation("Cross-platform Bluetooth initialization simulated");
        #endif
    }

    #if WINDOWS
    private void OnSubscribedClientsChanged(GattLocalCharacteristic sender, object args)
    {
        _subscribedClients = sender.SubscribedClients;
        _isConnected = _subscribedClients.Count > 0;
        var status = _isConnected ? "Connected" : "Disconnected";
        var deviceId = _isConnected ? _subscribedClients[0].Session.DeviceId.Id : "";

        OnConnectionStatusChanged(status, deviceId);
        _logger.LogInformation($"Subscribed clients changed. Count: {_subscribedClients.Count}");
    }

    private async void OnWriteRequested(GattLocalCharacteristic sender, GattWriteRequestedEventArgs args)
    {
        using var deferral = args.GetDeferral();
        var request = await args.GetRequestAsync();
        if (request == null)
        {
            _logger.LogWarning("Received a null write request.");
            return;
        }

        var reader = DataReader.FromBuffer(request.Value);
        var data = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(data);

        OnMessageReceived(DateTime.Now, data);

        if (request.Option == GattWriteOption.WriteWithResponse)
        {
            request.Respond();
        }
    }
    #endif

    /// <summary>
    /// Sends a notification to the connected controller via the notify characteristic.
    /// This is how the mock radio sends responses back to the controller application.
    /// </summary>
    /// <param name="data">The data to send</param>
    public async Task SendNotificationAsync(byte[] data)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Cannot send notification: No active connection");
        }

        try
        {
            _logger.LogInformation($"Sending notification: {data.Length} bytes");
            
            #if WINDOWS
            if (_notifyCharacteristic != null && _subscribedClients != null && _subscribedClients.Count > 0)
            {
                await _notifyCharacteristic.NotifyValueAsync(data.AsBuffer());
                _logger.LogInformation("Notification sent successfully via GATT.");
            }
            else
            {
                _logger.LogWarning("Cannot send notification, characteristic or subscribed clients not available.");
            }
            #else
            // In a real implementation, this would use the GATT NotifyCharacteristic
            // to send data to subscribed clients
            
            lock (_lock)
            {
                _notificationQueue.Add(data);
            }
            
            // Simulate async send operation
            await Task.Delay(50); // Simulate transmission delay
            
            _logger.LogInformation("Notification sent successfully (simulated)");
            #endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
            throw;
        }
    }

    /// <summary>
    /// Stops advertising and disconnects any active connections.
    /// </summary>
    public async Task StopAsync()
    {
        try
        {
            _logger.LogInformation("Stopping Bluetooth peripheral");
            
            #if WINDOWS
            if (_serviceProvider != null && _isAdvertising)
            {
                _serviceProvider.StopAdvertising();
            }
            _serviceProvider = null;
            
            if (_writeCharacteristic != null)
            {
                _writeCharacteristic.WriteRequested -= OnWriteRequested;
                _writeCharacteristic = null;
            }
            if (_notifyCharacteristic != null)
            {
                _notifyCharacteristic.SubscribedClientsChanged -= OnSubscribedClientsChanged;
                _notifyCharacteristic = null;
            }
            #endif

            if (_isConnected)
            {
                lock (_lock)
                {
                    _isConnected = false;
                }
                OnConnectionStatusChanged("Disconnected", "");
            }
            
            if (_isAdvertising)
            {
                _isAdvertising = false;
                OnConnectionStatusChanged("Stopped", "");
            }
            
            // Simulate async cleanup
            await Task.Delay(100);
            
            _logger.LogInformation("Bluetooth peripheral stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Bluetooth peripheral");
        }
    }

    /// <summary>
    /// Raises the MessageReceived event.
    /// </summary>
    protected virtual void OnMessageReceived(DateTime timestamp, byte[] data)
    {
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(timestamp, data));
    }

    /// <summary>
    /// Raises the ConnectionStatusChanged event.
    /// </summary>
    protected virtual void OnConnectionStatusChanged(string status, string deviceId)
    {
        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(status, deviceId));
    }
}
