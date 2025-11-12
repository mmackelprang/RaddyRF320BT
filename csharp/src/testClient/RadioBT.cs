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
    public bool IsHandshakeComplete { get; private set; }

    public RadioBT(IRadioTransport transport)
    {
        _transport = transport;
        _transport.NotificationReceived += OnNotification;
    }

    public async Task<bool> InitializeAsync(CancellationToken ct = default)
    {
        // Enable notifications assumed done externally (descriptor write)
        if (!await SendHandshake(ct)) return false;
        // Wait for ack success (poll inbound queue)
        var sw = DateTime.UtcNow;
        while ((DateTime.UtcNow - sw) < TimeSpan.FromSeconds(3))
        {
            if (_inboundFrames.TryPeek(out var f) && f.Group == CommandGroup.Ack && f.CommandId == 0x01)
            {
                IsHandshakeComplete = true;
                return true;
            }
            await Task.Delay(50, ct);
        }
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
        if (RadioFrame.TryParse(data, out var frame))
        {
            _inboundFrames.Enqueue(frame);
            if (frame.Group == CommandGroup.Button && frame.CommandId == 0x1C && data.Length >= 5) // heartbeat marker family
            {
                _lastHeartbeat = DateTime.UtcNow;
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
