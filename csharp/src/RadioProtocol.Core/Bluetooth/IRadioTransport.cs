using System;
using System.Threading.Tasks;

namespace RadioProtocol.Core.Bluetooth;

/// <summary>
/// Transport interface for radio communication
/// </summary>
public interface IRadioTransport : IDisposable
{
    /// <summary>
    /// Event fired when data is received from the radio
    /// </summary>
    event EventHandler<byte[]>? NotificationReceived;
    
    /// <summary>
    /// Writes data to the radio
    /// </summary>
    /// <param name="data">Data to write</param>
    /// <returns>True if write was successful</returns>
    Task<bool> WriteAsync(byte[] data);
}
