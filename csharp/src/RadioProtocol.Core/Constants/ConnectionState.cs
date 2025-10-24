namespace RadioProtocol.Core.Constants;

/// <summary>
/// Connection state for radio communication
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting,
    DiscoveringServices,
    Ready,
    Error
}