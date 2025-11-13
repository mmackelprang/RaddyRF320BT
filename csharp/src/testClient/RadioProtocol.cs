using System;
using System.Collections.Generic;
using System.Globalization;

namespace RadioClient;

public enum CommandGroup : byte { Button = 0x0C, Ack = 0x12, Status = 0x1C }

public static class CommandBase
{
    public const byte Header = 0xAB;
    public const byte Proto = 0x02;
    public static byte BaseFor(CommandGroup g) => g switch
    {
        CommandGroup.Button => 0xB9,
        CommandGroup.Ack => 0xBF,
        CommandGroup.Status => 0x00, // Status messages don't use standard checksum
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

    public static bool TryParse(ReadOnlySpan<byte> data, out RadioFrame? frame)
    {
        frame = default;
        
        // Handshake (length 4): AB 01 FF AB
        if (data.Length == 4 && data[0] == 0xAB && data[1] == 0x01 && data[3] == 0xAB)
        {
            frame = new RadioFrame(data[0], data[1], 0, data[2], data[3]);
            return true;
        }
        
        // Standard 5-byte frame (Button/Ack groups)
        if (data.Length == 5 && data[0] == CommandBase.Header && data[1] == CommandBase.Proto)
        {
            var group = (CommandGroup)data[2];
            if (group == CommandGroup.Button || group == CommandGroup.Ack)
            {
                byte baseVal = CommandBase.BaseFor(group);
                byte cmd = data[3];
                byte check = data[4];
                if (check != (byte)(baseVal + cmd)) return false;
                frame = new RadioFrame(data[0], data[1], group, cmd, check);
                return true;
            }
        }
        
        // Status messages: Variable length (AB-0X-1C-...) where 0X is length indicator
        if (data.Length >= 5 && data[0] == CommandBase.Header && data[2] == (byte)CommandGroup.Status)
        {
            byte lengthByte = data[1];
            // Create a status frame (proto field repurposed as length for status messages)
            frame = new RadioFrame(data[0], lengthByte, CommandGroup.Status, data[3], 
                data.Length > 4 ? data[4] : (byte)0);
            return true;
        }
        
        return false;
    }
}

public record StatusMessage(byte Type, string Label, string Value, byte[] RawData)
{
    public static StatusMessage? Parse(byte[] data)
    {
        // Status format: AB-LEN-1C-TYPE-03-DATALEN-[ASCII DATA]-CHECKSUM
        if (data.Length < 7 || data[0] != 0xAB || data[2] != 0x1C) return null;
        
        byte type = data[3];
        // data[4] is usually 0x03 (separator/sub-field)
        byte dataLength = data[5]; // Actual data length field
        
        // Extract ASCII data (starts at offset 6)
        if (6 + dataLength > data.Length) return null;
        
        byte[] asciiBytes = data[6..(6 + dataLength)];
        string value = System.Text.Encoding.ASCII.GetString(asciiBytes);
        
        string label = type switch
        {
            0x01 => "Demodulation",
            0x02 => "ModulationMode", // AM/NFM/WFM (not band name like AIR/VHF)
            0x03 => "BandWidth",
            0x04 => "Unknown04",
            0x05 => "SNR",
            0x06 => "FreqFractional1", // Fractional frequency part (single digit)
            0x07 => "RSSI",
            0x08 => "FreqFractional23", // Fractional frequency part (two digits)
            0x09 => "VolumeLabel",
            0x0A => "VolumeValue",
            0x0B => "Model",
            0x0C => "Status",
            0x10 => "Recording",
            _ => $"Type{type:X2}"
        };
        
        return new StatusMessage(type, label, value, data);
    }
}

public record RadioState(
    string RawHex,
    string FrequencyHex,
    double FrequencyMHz,
    bool UnitIsMHz,
    byte HighNibble,
    byte LowNibble,
    uint RawFreqValue = 0,
    byte ScaleFactor = 0,
    byte BandCode = 0,
    string BandName = "",
    byte SignalStrength = 0,    // High nibble of Byte9 (0-6, signal bars)
    byte SignalBars = 0          // Low nibble of Byte9 (additional signal info)
){
    // Get signal quality description from signal strength level (0-6)
    public static string GetSignalQuality(byte signalStrength) => signalStrength switch
    {
        0 => "No Signal",
        1 => "Very Weak",
        2 => "Weak",
        3 => "Fair",
        4 => "Good",
        5 => "Very Good",
        6 => "Excellent",
        _ => $"Unknown({signalStrength})"
    };
    
    public string SignalQualityText => GetSignalQuality(SignalStrength);
    // Get verified band name from band code (Byte 3 in ab0901 messages)
    public static string GetBandName(byte bandCode) => bandCode switch
    {
        0x00 => "FM",      // FM Radio (87.5-108 MHz)
        0x01 => "MW",      // Medium Wave / AM Radio (530-1710 KHz)
        0x02 => "SW",      // Short Wave
        0x03 => "AIR",     // Airband / Aviation (108-137 MHz)
        0x06 => "WB",      // Weather Band (162-163 MHz)
        0x07 => "VHF",     // VHF Band (136-174 MHz)
        _ => $"Unknown({bandCode:X2})"
    };
    public static RadioState? Parse(byte[] value)
    {
        string hex = BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();
        if (hex.Length < 22) return null; // Need at least 11 bytes for ab0901
        string sig = hex[..6];
        if (sig == "ab0901")
        {
            // Format: AB-09-01-B3-B4B5-B6-0000-B9-00-CK
            // where B3-B4B5 = 24-bit frequency value, B9 = scale factor
            if (hex.Length < 22) return null;
            
            // Extract band code (Byte 3)
            string byte3 = hex.Substring(6, 2);   // Byte 3 - Band code
            byte bandCode = byte.Parse(byte3, NumberStyles.HexNumber);
            string bandName = GetBandName(bandCode);
            
            // Extract 24-bit frequency (Byte3 + Byte4 + Byte5)
            // Note: Byte3 serves dual purpose - band code AND part of frequency value
            string byte4 = hex.Substring(8, 2);   // Byte 4
            string byte5 = hex.Substring(10, 2);  // Byte 5
            string freqHex = byte3 + byte4 + byte5;
            uint rawFreq = Convert.ToUInt32(freqHex, 16);
            
            // Extract Byte 9 (contains signal strength in nibbles)
            // Format: High nibble = signal strength (0-6), Low nibble = signal bars/mode
            string byte9Hex = hex.Substring(18, 2);
            byte byte9 = byte.Parse(byte9Hex, NumberStyles.HexNumber);
            byte signalStrength = (byte)((byte9 >> 4) & 0x0F);  // High nibble
            byte signalBars = (byte)(byte9 & 0x0F);             // Low nibble
            
            // Note: The full byte9 value is still used as "scale factor" in frequency calc
            byte scaleFactor = byte9;
            
            // Calculate frequency with scale factor (approximate only)
            double freq = ScaleFrequency(rawFreq, scaleFactor);
            
            byte first = byte.Parse(byte3, NumberStyles.HexNumber);
            byte high = (byte)(first >> 4);
            byte low = (byte)(first & 0x0F);
            
            // Byte 6 might indicate unit (00=MHz, 01=kHz, 02=?)
            string unitByte = hex.Substring(12, 2);
            bool isMHz = unitByte == "00" || unitByte == "01" || unitByte == "02";
            
            return new RadioState(hex, freqHex, freq, isMHz, high, low, rawFreq, scaleFactor, 
                bandCode, bandName, signalStrength, signalBars);
        }
        // Extended variant placeholder
        if (sig == "ab090f")
        {
            // Layout differs; adjust once full trace captured.
            return null; // unsupported until confirmed
        }
        return null;
    }

    private static double ScaleFrequency(uint raw, byte scaleFactor = 0)
    {
        // ⚠️ FREQUENCY ENCODING NOT FULLY DECODED ⚠️
        //
        // Verified data from RF320-BLE hardware testing (Nov 13, 2025):
        // Band | Display Freq | Raw (B3+B4+B5) | Byte6 | Byte9(Scale) | Divisor
        // -----|--------------|----------------|-------|--------------|----------
        // MW   | 1.270 MHz    | 0x01F604       | 0x00  | 0x30 (48)    | ~101,194
        // FM   | 102.30 MHz   | 0x00F627       | 0x00  | 0x24 (36)    | ~616
        // AIR  | 119.345 MHz  | 0x0331D2       | 0x01  | 0x13 (19)    | ~1,754
        // WB   | 162.40 MHz   | 0x06607A       | 0x02  | 0x13 (19)    | ~2,573
        // VHF  | 145.095 MHz  | 0x07C736       | 0x02  | 0x13 (19)    | ~3,521
        //
        // Analysis from STATUS_MESSAGE_ANALYSIS.md (Android app reverse engineering):
        // - Android extracts 4 frequency bytes, concatenates them, calls hexToDec()
        // - However, obfuscated variable names hide exact byte positions
        // - Android messages may be longer (14+ bytes) vs our 12-byte messages
        // 
        // CONCLUSION: The divisor varies even with same scale factor AND byte6 value.
        // This suggests either:
        // 1. Additional encoding in other bytes (B7, B8, or nibbles within B9)
        // 2. Non-linear formula or lookup table
        // 3. Different message format between firmware versions
        //
        // Current implementation provides APPROXIMATE values only.
        // Display should show raw hex values for analysis until formula is decoded.
        
        if (scaleFactor == 0)
        {
            // Fallback: Adaptive heuristic for legacy messages
            if (raw > 2_000_000) return raw / 100_000.0;
            if (raw > 200_000) return raw / 1_000.0;
            if (raw > 10_000) return raw / 100.0;
            return raw / 10.0;
        }
        
        // Approximate formula (not accurate, needs more reverse engineering):
        // Returns values in ballpark but not exact
        double approxFreq = raw / (scaleFactor * 100.0);
        
        // Note: This will be inaccurate. Frequency display should show raw value
        // until proper decoding formula is determined.
        return approxFreq;
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
