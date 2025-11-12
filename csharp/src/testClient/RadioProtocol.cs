using System;
using System.Collections.Generic;
using System.Globalization;

namespace RadioClient;

public enum CommandGroup : byte { Button = 0x0C, Ack = 0x12 }

public static class CommandBase
{
    public const byte Header = 0xAB;
    public const byte Proto = 0x02;
    public static byte BaseFor(CommandGroup g) => g switch
    {
        CommandGroup.Button => 0xB9,
        CommandGroup.Ack => 0xBF,
        _ => throw new ArgumentOutOfRangeException(nameof(g))
    };
}

public enum CanonicalAction
{
    Band, Number1, Number2, Number3, Number4, Number5, Number6, Number7, Number8, Number9, Number0,
    Back, Point, FreqConfirm, UpShort, UpLong, DownShort, DownLong, VolAdd, VolDel, Power,
    SubBand, Music, Play, LongPlay, Step, Circle, MusicCycle, Bluetooth, Demodulation,
    BandWidth, MobileDisplay, SQ, Stereo, DeEmphasis, Preset, Memo, Rec, BandLong,
    SOSShort, SOSLong, RecClick, MemoLong, StepAlt, FourClick, FiveClick, AlarmDadunClick, AlarmDadunLong,
    FuncLong,
    // Long numerics / specials (0x35-0x49)
    Num1Hold, Num2Hold, Num3Hold, Num4Hold, Num5Hold, Num6Hold, Num7Hold, Num8Hold, Num9Hold, Num0Hold,
    MusicHold, PlayHold, ModeHold, EqHold, DecHold, IncHold, PowerHold, EnterHold, PointHold, DeleteHold, MemoHold,
    AckSuccess, AckFail,
}

public static class CommandIdMap
{
    // Canonical mapping (duplicates collapsed)
    public static readonly Dictionary<CanonicalAction, byte> Id = new()
    {
        { CanonicalAction.Band, 0x00 },
        { CanonicalAction.Number1, 0x01 }, { CanonicalAction.Number2, 0x02 }, { CanonicalAction.Number3, 0x03 },
        { CanonicalAction.Number4, 0x04 }, { CanonicalAction.Number5, 0x05 }, { CanonicalAction.Number6, 0x06 },
        { CanonicalAction.Number7, 0x07 }, { CanonicalAction.Number8, 0x08 }, { CanonicalAction.Number9, 0x09 },
        { CanonicalAction.Number0, 0x0A }, { CanonicalAction.Back, 0x0B }, { CanonicalAction.Point, 0x0C },
        { CanonicalAction.FreqConfirm, 0x0D }, { CanonicalAction.UpShort, 0x0E }, { CanonicalAction.UpLong, 0x0F },
        { CanonicalAction.DownShort, 0x10 }, { CanonicalAction.DownLong, 0x11 }, { CanonicalAction.VolAdd, 0x12 },
        { CanonicalAction.VolDel, 0x13 }, { CanonicalAction.Power, 0x14 },
        { CanonicalAction.SubBand, 0x17 }, { CanonicalAction.Play, 0x1A }, { CanonicalAction.LongPlay, 0x33 },
        { CanonicalAction.Step, 0x1B }, { CanonicalAction.Circle, 0x27 }, { CanonicalAction.MusicCycle, 0x28 },
        { CanonicalAction.Bluetooth, 0x1C }, { CanonicalAction.Demodulation, 0x1D }, { CanonicalAction.BandWidth, 0x1E },
        { CanonicalAction.MobileDisplay, 0x1F }, { CanonicalAction.SQ, 0x20 }, { CanonicalAction.Stereo, 0x21 },
        { CanonicalAction.DeEmphasis, 0x22 }, { CanonicalAction.Preset, 0x23 }, { CanonicalAction.Memo, 0x24 },
        { CanonicalAction.Rec, 0x25 }, { CanonicalAction.Music, 0x26 }, { CanonicalAction.BandLong, 0x29 },
        { CanonicalAction.SOSShort, 0x2A }, { CanonicalAction.SOSLong, 0x2B }, { CanonicalAction.MemoLong, 0x2C },
        { CanonicalAction.RecClick, 0x2D }, { CanonicalAction.StepAlt, 0x2E }, { CanonicalAction.FourClick, 0x2F },
        { CanonicalAction.FiveClick, 0x30 }, { CanonicalAction.AlarmDadunClick, 0x31 }, { CanonicalAction.AlarmDadunLong, 0x32 },
        { CanonicalAction.FuncLong, 0x34 },
        // Holds 0x35-0x49
        { CanonicalAction.Num1Hold, 0x35 }, { CanonicalAction.Num2Hold, 0x36 }, { CanonicalAction.Num3Hold, 0x37 },
        { CanonicalAction.Num4Hold, 0x38 }, { CanonicalAction.Num5Hold, 0x39 }, { CanonicalAction.Num6Hold, 0x3A },
        { CanonicalAction.Num7Hold, 0x3B }, { CanonicalAction.Num8Hold, 0x3C }, { CanonicalAction.Num9Hold, 0x3D },
        { CanonicalAction.Num0Hold, 0x3E }, { CanonicalAction.MusicHold, 0x3F }, { CanonicalAction.PlayHold, 0x40 },
        { CanonicalAction.ModeHold, 0x41 }, { CanonicalAction.EqHold, 0x42 }, { CanonicalAction.DecHold, 0x43 },
        { CanonicalAction.IncHold, 0x44 }, { CanonicalAction.PowerHold, 0x45 }, { CanonicalAction.EnterHold, 0x46 },
        { CanonicalAction.PointHold, 0x47 }, { CanonicalAction.DeleteHold, 0x48 }, { CanonicalAction.MemoHold, 0x49 },
        { CanonicalAction.AckFail, 0x00 }, { CanonicalAction.AckSuccess, 0x01 },
    };
}

public record RadioFrame(byte Header, byte Proto, CommandGroup Group, byte CommandId, byte Check)
{
    public static RadioFrame Build(CommandGroup group, byte cmdId)
    {
        byte check = (byte)(CommandBase.BaseFor(group) + cmdId);
        return new RadioFrame(CommandBase.Header, CommandBase.Proto, group, cmdId, check);
    }

    public byte[] ToBytes() => new[] { Header, Proto, (byte)Group, CommandId, Check };

    public static bool TryParse(ReadOnlySpan<byte> data, out RadioFrame frame)
    {
        frame = default;
        if (data.Length == 5 && data[0] == CommandBase.Header && data[1] == CommandBase.Proto)
        {
            var group = (CommandGroup)data[2];
            byte baseVal = CommandBase.BaseFor(group);
            byte cmd = data[3];
            byte check = data[4];
            if (check != (byte)(baseVal + cmd)) return false;
            frame = new RadioFrame(data[0], data[1], group, cmd, check);
            return true;
        }
        // Handshake (length 4): AB 01 FF AB
        if (data.Length == 4 && data[0] == 0xAB && data[1] == 0x01 && data[3] == 0xAB)
        {
            frame = new RadioFrame(data[0], data[1], 0, data[2], data[3]);
            return true;
        }
        return false;
    }
}

public record RadioState(
    string RawHex,
    string FrequencyHex,
    double FrequencyMHz,
    bool UnitIsMHz,
    byte HighNibble,
    byte LowNibble
){
    public static RadioState? Parse(byte[] value)
    {
        string hex = BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
        if (hex.Length < 12) return null;
        string sig = hex[..6];
        if (sig == "ab0901")
        {
            // heuristic extraction
            if (hex.Length < 20) return null;
            string freqPart1 = hex.Substring(8, 2);
            string freqPart2 = hex.Substring(10, 2);
            string freqPart3 = hex.Substring(12, 2);
            string freqPart4 = hex.Substring(14, 2);
            string freqHex = freqPart1 + freqPart2 + freqPart3 + freqPart4;
            byte first = byte.Parse(freqPart1, NumberStyles.HexNumber);
            byte high = (byte)(first >> 4);
            byte low = (byte)(first & 0x0F);
            double freq = ScaleFrequency(Convert.ToUInt32(freqHex, 16));
            string unitByte = hex.Substring(16, 2);
            bool isMHz = unitByte == "00"; // 00 -> MHz, 01 -> KHz (per smali runnables)
            return new RadioState(hex, freqHex, freq, isMHz, high, low);
        }
        // Extended variant placeholder
        if (sig == "ab090f")
        {
            // Layout differs; adjust once full trace captured.
            return null; // unsupported until confirmed
        }
        return null;
    }

    private static double ScaleFrequency(uint raw)
    {
        // Adaptive heuristic described in PROTOCOL_INFO.md
        if (raw > 2_000_000) return raw / 100_000.0;
        if (raw > 200_000) return raw / 1_000.0;
        if (raw > 10_000) return raw / 100.0;
        return raw / 10.0;
    }
}

public static class FrameFactory
{
    public static byte[] Build(CanonicalAction action)
    {
        var id = CommandIdMap.Id[action];
        CommandGroup group = action is CanonicalAction.AckSuccess or CanonicalAction.AckFail
            ? CommandGroup.Ack
            : CommandGroup.Button;
        return RadioFrame.Build(group, id).ToBytes();
    }

    public static byte[] Handshake() => new byte[] { 0xAB, 0x01, 0xFF, 0xAB };
}
