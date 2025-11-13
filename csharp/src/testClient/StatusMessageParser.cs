using System;

namespace RadioClient;

public static class StatusMessageParser
{
    public static string ParseStatus(byte[] data)
    {
        if (data.Length < 4 || data[0] != 0xAB || data[2] != 0x1C)
            return "Unknown status format";
        
        byte lengthByte = data[1];
        byte subType = data[3];
        
        return subType switch
        {
            0x06 when data.Length >= 7 => ParseStatus06(data),
            0x08 when data.Length >= 9 => ParseStatus08(data),
            _ => $"Status subtype 0x{subType:X2} ({data.Length} bytes): {BitConverter.ToString(data)}"
        };
    }
    
    private static string ParseStatus06(byte[] data)
    {
        // Format: AB-05-1C-06-03-01-XX-YY
        // Bytes: 0   1   2   3   4   5   6   7
        if (data.Length < 8)
            return $"Status 0x06 (short): {BitConverter.ToString(data)}";
        
        byte b4 = data[4]; // 03
        byte b5 = data[5]; // 01
        byte b6 = data[6]; // varies (30-33)
        byte b7 = data[7]; // varies (checksum?)
        
        return $"Status06: mode={b4:X2} sub={b5:X2} val={b6:X2} chk={b7:X2}";
    }
    
    private static string ParseStatus08(byte[] data)
    {
        // Format: AB-06-1C-08-03-02-XX-XX-YY
        // Bytes: 0   1   2   3   4   5   6   7   8
        if (data.Length < 9)
            return $"Status 0x08 (short): {BitConverter.ToString(data)}";
        
        byte b4 = data[4]; // 03
        byte b5 = data[5]; // 02
        byte b6 = data[6]; // varies (31-32)
        byte b7 = data[7]; // varies (30-39)
        byte b8 = data[8]; // varies (checksum?)
        
        // Might be 2-digit values or encoded data
        char c1 = (char)b6;
        char c2 = (char)b7;
        
        return $"Status08: mode={b4:X2} sub={b5:X2} data={c1}{c2} (0x{b6:X2}{b7:X2}) chk={b8:X2}";
    }
}
