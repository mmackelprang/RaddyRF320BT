using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadioClient;

public sealed class RadioBT : IDisposable
{
    private readonly IRadioTransport _transport;
    private readonly ConcurrentQueue<RadioFrame> _inboundFrames = new();
    private readonly ConcurrentQueue<RadioState> _stateSnapshots = new();
    private readonly TimeSpan _heartbeatThreshold = TimeSpan.FromSeconds(2);
    private DateTime _lastHeartbeat = DateTime.UtcNow;
    private CancellationTokenSource? _cts;

    public event EventHandler<RadioFrame>? FrameReceived;
    public event EventHandler<RadioState>? StateUpdated;
    public event EventHandler<StatusMessage>? StatusReceived;
    public bool IsHandshakeComplete { get; private set; }

    public RadioBT(IRadioTransport transport)
    {
        _transport = transport;
        _transport.NotificationReceived += OnNotification;
    }

    public async Task<bool> InitializeAsync(CancellationToken ct = default)
    {
        // Send handshake - device responds with status stream, not ACK
        if (!await SendHandshake(ct)) return false;
        
        // Wait briefly for any response (status messages indicate connection is active)
        var sw = DateTime.UtcNow;
        while ((DateTime.UtcNow - sw) < TimeSpan.FromSeconds(1))
        {
            // Accept any frame or state update as evidence of successful connection
            if (_inboundFrames.Count > 0 || _stateSnapshots.Count > 0)
            {
                IsHandshakeComplete = true;
                return true;
            }
            await Task.Delay(50, ct);
        }
        Console.WriteLine("No response from device within timeout");
        return false;
    }

    private async Task<bool> SendHandshake(CancellationToken ct)
    {
        return await _transport.WriteAsync(FrameFactory.Handshake());
    }

    public async Task<bool> SendAsync(CanonicalAction action)
    {
        var bytes = FrameFactory.Build(action);
        return await _transport.WriteAsync(bytes);
    }

    private void OnNotification(object? sender, byte[] data)
    {
        if (RadioFrame.TryParse(data, out var frame) && frame != null)
        {
            _inboundFrames.Enqueue(frame);
            
            // Check for status messages (Group 0x1C)
            if (frame.Group == CommandGroup.Status)
            {
                _lastHeartbeat = DateTime.UtcNow;
                
                // Try to parse as StatusMessage for detailed info
                var status = StatusMessage.Parse(data);
                if (status != null)
                {
                    StatusReceived?.Invoke(this, status);
                }
            }
            
            FrameReceived?.Invoke(this, frame);
            return;
        }
        
        var state = RadioState.Parse(data);
        if (state != null)
        {
            _stateSnapshots.Enqueue(state);
            StateUpdated?.Invoke(this, state);
        }
    }

    public bool IsHeartbeatAlive => DateTime.UtcNow - _lastHeartbeat < _heartbeatThreshold;

    public void StartMonitor()
    {
        _cts = new CancellationTokenSource();
        _ = MonitorLoop(_cts.Token);
    }

    private async Task MonitorLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!IsHeartbeatAlive && IsHandshakeComplete)
            {
                // Strategy: optionally resend handshake to re-sync.
                await SendHandshake(ct);
            }
            await Task.Delay(500, ct);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _transport.NotificationReceived -= OnNotification;
        _transport.Dispose();
    }
}
