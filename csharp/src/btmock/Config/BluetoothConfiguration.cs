namespace BtMock.Config;

/// <summary>
/// Configuration settings for the Bluetooth LE peripheral.
/// These settings define the device identity and GATT service characteristics.
/// All UUIDs and identifiers can be easily modified via config.json.
/// </summary>
public class BluetoothConfiguration
{
    /// <summary>
    /// The advertised Bluetooth device name (e.g., "RF320-BLE").
    /// This is how the device will appear to scanning applications.
    /// </summary>
    public string DeviceName { get; set; } = "RF320-BLE";

    /// <summary>
    /// The Bluetooth device address/MAC address.
    /// Format: XX:XX:XX:XX:XX:XX (e.g., "D5:D6:2A:FF:42:41")
    /// Note: Actual address may be managed by the OS on some platforms.
    /// </summary>
    public string DeviceAddress { get; set; } = "D5:D6:2A:FF:42:41";

    /// <summary>
    /// The GATT Service UUID that contains the communication characteristics.
    /// Format: Standard UUID string (e.g., "0000ff10-0000-1000-8000-00805f9b34fb")
    /// </summary>
    public string ServiceUUID { get; set; } = "0000ff10-0000-1000-8000-00805f9b34fb";

    /// <summary>
    /// The GATT Write Characteristic UUID for receiving data from the controller.
    /// Controllers write commands to this characteristic.
    /// Format: Standard UUID string (e.g., "0000ff13-0000-1000-8000-00805f9b34fb")
    /// </summary>
    public string WriteCharacteristicUUID { get; set; } = "0000ff13-0000-1000-8000-00805f9b34fb";

    /// <summary>
    /// The GATT Notify Characteristic UUID for sending data to the controller.
    /// This mock device sends responses via notifications on this characteristic.
    /// Format: Standard UUID string (e.g., "0000ff14-0000-1000-8000-00805f9b34fb")
    /// </summary>
    public string NotifyCharacteristicUUID { get; set; } = "0000ff14-0000-1000-8000-00805f9b34fb";
}
