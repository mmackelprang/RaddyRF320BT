using System;
using System.Globalization;

namespace RadioProtocol.Core.Protocol;

/// <summary>
/// Represents a status message from the radio
/// </summary>
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

/// <summary>
/// Represents the radio's current state (frequency, band, signal strength, etc.)
/// </summary>
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
)
{
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
            
            // For MW band (KHz), keep the value as KHz (don't convert to MHz)
            // For other bands, the frequency is already in MHz
            // The isKHz flag should remain false for MW since we're keeping it as KHz
            if (isKHz)
            {
                // MW band: frequency is in KHz, don't convert
                // freq stays as-is (e.g., 1270 KHz)
            }
            else
            {
                // Other bands: frequency is in MHz
                // freq stays as-is (e.g., 102.30 MHz)
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
}
