namespace BtMock.Bluetooth;

/// <summary>
/// Event arguments for connection status change events.
/// </summary>
public class ConnectionStatusEventArgs : EventArgs
{
    /// <summary>
    /// The current connection status.
    /// </summary>
    public string Status { get; init; }

    /// <summary>
    /// The identifier of the connected device (if applicable).
    /// </summary>
    public string DeviceId { get; init; }

    public ConnectionStatusEventArgs(string status, string deviceId = "")
    {
        Status = status;
        DeviceId = deviceId;
    }
}
