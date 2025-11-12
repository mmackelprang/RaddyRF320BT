using System;
using System.Threading.Tasks;

namespace RadioClient;

public interface IRadioTransport : IDisposable
{
    Task<bool> WriteAsync(byte[] data);
    event EventHandler<byte[]>? NotificationReceived;
}
