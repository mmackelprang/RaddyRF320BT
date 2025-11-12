# RadioClient Wrapper

Protocol implementation for the Radio-C BLE device (reverse-engineered).

## Features
- Frame builder (button + ack groups) with checksum auto-generation.
- Handshake send + success detection.
- Adaptive frequency parsing for `ab0901` snapshot frames.
- Heartbeat monitoring via `0x1C` status frames.
- Collision-safe canonical action enum.

## Project Structure
- `RadioClient.csproj` .NET 8.0 SDK project.
- `IRadioTransport.cs` abstraction for BLE I/O (implement per platform).
- `RadioProtocol.cs` frame & state logic.
- `RadioBT.cs` high-level session manager.

## Usage Sketch
```csharp
IRadioTransport transport = /* platform implementation */;
var radio = new RadioBT(transport);
if (await radio.InitializeAsync())
{
    await radio.SendAsync(CanonicalAction.Power); // toggle power
}
radio.StateUpdated += (_, st) => Console.WriteLine($"Freq: {st.FrequencyMHz:0.000} MHz (raw {st.FrequencyHex})");
```

## Implementing IRadioTransport (Pseudo)
```csharp
public sealed class WinRtBleTransport : IRadioTransport
{
    public event EventHandler<byte[]>? NotificationReceived;
    public async Task<bool> WriteAsync(byte[] data){ /* write to ff13 */ return true; }
    public void Dispose(){ /* dispose gatt */ }
}
```

## Next Steps
- Capture full-length frames for all `abxx1c` signatures.
- Refine frequency scaling once raw vs displayed comparison available.
- Add support for `ab090f` alternate snapshot layout.
