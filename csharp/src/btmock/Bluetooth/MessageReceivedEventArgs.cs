namespace BtMock.Bluetooth;

/// <summary>
/// Event arguments for message received events.
/// Contains the timestamp and raw data of received messages.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// The timestamp when the message was received.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The raw byte data of the received message.
    /// </summary>
    public byte[] Data { get; init; }

    public MessageReceivedEventArgs(DateTime timestamp, byte[] data)
    {
        Timestamp = timestamp;
        Data = data;
    }
}
