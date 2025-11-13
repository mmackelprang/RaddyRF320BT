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
    
    // Get decimal places for frequency display based on band
    private static int GetDecimalPlaces(byte bandCode) => bandCode switch
    {
        0x00 => 2,  // FM: 2 decimal places (e.g., 102.30 MHz)
        0x01 => 0,  // MW: 0 decimal places (e.g., 1270 KHz)
        _ => 3      // All other bands: 3 decimal places (e.g., 119.345 MHz)
    };
    
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
            
            // Extract frequency bytes (Bytes 4-7) for nibble-based decoding
            string byte4Str = hex.Substring(8, 2);   // Byte 4
            string byte5Str = hex.Substring(10, 2);  // Byte 5
            string byte6Str = hex.Substring(12, 2);  // Byte 6
            string byte7Str = hex.Substring(14, 2);  // Byte 7
            
            byte byte4 = byte.Parse(byte4Str, NumberStyles.HexNumber);
            byte byte5 = byte.Parse(byte5Str, NumberStyles.HexNumber);
            byte byte6 = byte.Parse(byte6Str, NumberStyles.HexNumber);
            byte byte7 = byte.Parse(byte7Str, NumberStyles.HexNumber);
            
            // Extract Byte 8 (unit indicator: 0=MHz, 1=KHz)
            string byte8Str = hex.Substring(16, 2);
            byte byte8 = byte.Parse(byte8Str, NumberStyles.HexNumber);
            bool isKHz = byte8 == 0x01;
            
            // Extract Byte 9 (contains signal strength in nibbles)
            // Format: High nibble = signal strength (0-6), Low nibble = signal bars/mode
            string byte9Hex = hex.Substring(18, 2);
            byte byte9 = byte.Parse(byte9Hex, NumberStyles.HexNumber);
            byte signalStrength = (byte)((byte9 >> 4) & 0x0F);  // High nibble
            byte signalBars = (byte)(byte9 & 0x0F);             // Low nibble
            
            // Decode frequency using nibble extraction method
            // Formula: Extract nibbles from bytes 4-7, assemble as: B6L B5H B5L B4H B4L
            byte b4High = (byte)((byte4 >> 4) & 0x0F);
            byte b4Low = (byte)(byte4 & 0x0F);
            byte b5High = (byte)((byte5 >> 4) & 0x0F);
            byte b5Low = (byte)(byte5 & 0x0F);
            byte b6Low = (byte)(byte6 & 0x0F);
            
            // Assemble frequency hex string from nibbles
            string freqHex = $"{b6Low:X}{b5High:X}{b5Low:X}{b4High:X}{b4Low:X}";
            uint freqRaw = Convert.ToUInt32(freqHex, 16);
            
            // Apply decimal places based on band
            int decimalPlaces = GetDecimalPlaces(bandCode);
            double freq = freqRaw / Math.Pow(10, decimalPlaces);
            
            // Convert KHz to MHz if necessary
            if (isKHz)
            {
                freq = freq / 1000.0;  // Convert KHz to MHz for consistent display
            }
            
            // Legacy fields for compatibility
            byte scaleFactor = byte9;  // Kept for logging, not used in new formula
            uint rawFreq = (uint)((bandCode << 16) | (byte4 << 8) | byte5);  // Old format for logging
            byte first = bandCode;
            byte high = (byte)(first >> 4);
            byte low = (byte)(first & 0x0F);
            
            return new RadioState(hex, freqHex, freq, !isKHz, high, low, freqRaw, scaleFactor, 
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

    // Note: ScaleFrequency method removed - replaced with correct nibble-based decoding
    // Frequency decoding is now handled directly in Parse() method
    // 
    // ✅ FREQUENCY ENCODING FULLY DECODED (Nov 13, 2025)
    //
    // Breakthrough: Frequency is encoded in nibbles of bytes 4-7:
    // 1. Extract nibbles: B4High, B4Low, B5High, B5Low, B6Low
    // 2. Assemble hex string: B6Low + B5High + B5Low + B4High + B4Low
    // 3. Convert to decimal
    // 4. Apply decimal places: FM=2, MW/AM=0, Others=3
    // 5. Check Byte 8: 0=MHz, 1=KHz (convert KHz to MHz for display)
    //
    // Verified against hardware data:
    // Band | Display      | B4   B5   B6   B7   B8 | Nibbles    | Result
    // -----|--------------|------------------------|------------|-------------
    // MW   | 1.270 MHz    | F6   04   00   00   01 | 0 0 4 F 6  | 1270 KHz ✓
    // FM   | 102.30 MHz   | F6   27   00   00   00 | 0 2 7 F 6  | 10230 ✓
    // AIR  | 119.345 MHz  | 31   D2   01   00   00 | 1 D 2 3 1  | 119345 ✓
    // WB   | 162.40 MHz   | 60   7A   02   00   00 | 2 7 A 6 0  | 162400 ✓
    // VHF  | 145.095 MHz  | C7   36   02   00   00 | 2 3 6 C 7  | 145095 ✓

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
