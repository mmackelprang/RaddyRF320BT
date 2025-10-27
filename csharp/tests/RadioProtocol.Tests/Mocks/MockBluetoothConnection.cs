using RadioProtocol.Core.Bluetooth;
using RadioProtocol.Core.Constants;
using RadioProtocol.Core.Models;
using BluetoothDeviceInfo = RadioProtocol.Core.Bluetooth.DeviceInfo;

namespace RadioProtocol.Tests.Mocks;

/// <summary>
/// Mock Bluetooth connection for testing
/// </summary>
public class MockBluetoothConnection : IBluetoothConnection
{
    private readonly Queue<byte[]> _responseQueue = new();
    private readonly List<byte[]> _sentCommands = new();
    private ConnectionInfo _connectionStatus;
    private bool _isConnected;
    private bool _disposed;

    public event EventHandler<ConnectionInfo>? ConnectionStateChanged;
    public event EventHandler<byte[]>? DataReceived;

    public bool IsConnected => _isConnected;
    public ConnectionInfo ConnectionStatus => _connectionStatus;

    // Test helpers
    public IReadOnlyList<byte[]> SentCommands => _sentCommands.AsReadOnly();
    public int ResponseQueueCount => _responseQueue.Count;

    public MockBluetoothConnection()
    {
        _connectionStatus = new ConnectionInfo
        {
            State = ConnectionState.Disconnected,
            DeviceName = "Mock Radio Device",
            DeviceAddress = "00:11:22:33:44:55",
            Timestamp = DateTime.Now
        };
    }

    public Task<IEnumerable<BluetoothDeviceInfo>> ScanForDevicesAsync(CancellationToken cancellationToken = default)
    {
        // Return a mock device for testing
        var devices = new List<BluetoothDeviceInfo>
        {
            new BluetoothDeviceInfo("Mock Radio Device", "00:11:22:33:44:55"),
            new BluetoothDeviceInfo("Mock Radio Device 2", "11:22:33:44:55:66")
        };
        return Task.FromResult<IEnumerable<BluetoothDeviceInfo>>(devices);
    }

    public Task<bool> ConnectAsync(string deviceAddress, CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        _connectionStatus = _connectionStatus with 
        { 
            State = ConnectionState.Connected,
            DeviceAddress = deviceAddress,
            Timestamp = DateTime.Now
        };
        
        ConnectionStateChanged?.Invoke(this, _connectionStatus);
        return Task.FromResult(true);
    }

    public Task DisconnectAsync()
    {
        _isConnected = false;
        _connectionStatus = _connectionStatus with 
        { 
            State = ConnectionState.Disconnected,
            Timestamp = DateTime.Now
        };
        
        ConnectionStateChanged?.Invoke(this, _connectionStatus);
        return Task.CompletedTask;
    }

    public Task<bool> SendDataAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return Task.FromResult(false);

        _sentCommands.Add(data.ToArray());

        // Trigger any queued responses
        while (_responseQueue.TryDequeue(out var response))
        {
            Task.Run(() => DataReceived?.Invoke(this, response));
        }

        return Task.FromResult(true);
    }

    // Test helper methods
    public void QueueResponse(byte[] responseData)
    {
        _responseQueue.Enqueue(responseData.ToArray());
    }

    public void QueueResponse(string hexData)
    {
        var bytes = Convert.FromHexString(hexData);
        QueueResponse(bytes);
    }

    public void SimulateConnectionError(string errorMessage)
    {
        _isConnected = false;
        _connectionStatus = _connectionStatus with 
        { 
            State = ConnectionState.Error,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.Now
        };
        
        ConnectionStateChanged?.Invoke(this, _connectionStatus);
    }

    public void ClearSentCommands()
    {
        _sentCommands.Clear();
    }

    public void ClearResponseQueue()
    {
        _responseQueue.Clear();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _responseQueue.Clear();
            _sentCommands.Clear();
            _disposed = true;
        }
    }
}