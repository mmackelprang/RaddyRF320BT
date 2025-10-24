namespace RadioProtocol.Core.Constants;

/// <summary>
/// Button types for radio control
/// </summary>
public enum ButtonType : byte
{
    // Number buttons (0-9)
    Number0 = 0x0A,
    Number1 = 0x01,
    Number2 = 0x02,
    Number3 = 0x03,
    Number4 = 0x04,
    Number5 = 0x05,
    Number6 = 0x06,
    Number7 = 0x07,
    Number8 = 0x08,
    Number9 = 0x09,
    
    // Band and navigation
    Band = 0x00,
    SubBand = 0x17,
    BandLongPress = 0x29,
    Back = 0x0B,
    BACK = 0x0B,  // Alias for tests
    Point = 0x0C,
    Frequency = 0x0D,
    
    // Navigation arrows
    UpShort = 0x0E,
    UpLong = 0x0F,
    CHANNEL_UP = 0x0E,  // Alias for tests
    DownShort = 0x10,
    DownLong = 0x11,
    CHANNEL_DOWN = 0x10,  // Alias for tests
    
    // Volume control
    VolumeUp = 0x12,
    VOLUME_UP = 0x12,  // Alias for tests
    VolumeDown = 0x13,
    VOLUME_DOWN = 0x13,  // Alias for tests
    
    // Power and system
    Power = 0x14,
    POWER = 0x14,  // Alias for tests
    PowerLong = 0x45,
    Bluetooth = 0x1C,
    
    // Audio modes
    Music = 0x26,
    MusicLong = 0x3F,
    Play = 0x1A,
    PlayLong = 0x33,
    PlayModeLong = 0x40,
    Step = 0x1B,
    StepNew = 0x2E,
    Circle = 0x27,
    MusicTypeCircle = 0x28,
    
    // Radio settings
    Demodulation = 0x1D,
    Bandwidth = 0x1E,
    MobileDisplay = 0x1F,
    Squelch = 0x20,
    Stereo = 0x21,
    DeEmphasis = 0x22,
    
    // Memory and preset
    Preset = 0x23,
    Memo = 0x24,
    MemoLong = 0x2C,
    MeterLong = 0x49,
    
    // Recording
    Record = 0x25,
    RecordClick = 0x2D,
    
    // Emergency
    SOS = 0x2A,
    SOSLong = 0x2B,
    AlarmClick = 0x31,
    AlarmLong = 0x32,
    
    // Function keys
    FunctionLong = 0x34,
    FunctionKey1 = 0x1D,  // Same as Demodulation
    FunctionKey2 = 0x1E,  // Same as Bandwidth
    FunctionKey3 = 0x2E,  // Same as StepNew
    FunctionKey4 = 0x2F,
    FunctionKey5 = 0x30,
    
    // Number long press (memory channels)
    Number1Long = 0x35,
    Number2Long = 0x36,
    Number3Long = 0x37,
    Number4Long = 0x38,
    Number5Long = 0x39,
    Number6Long = 0x3A,
    Number7Long = 0x3B,
    Number8Long = 0x3C,
    Number9Long = 0x3D,
    Number0Long = 0x3E,  // TEN
    
    // Numeric keypad special functions
    NumericModeLong = 0x41,
    EqualsLong = 0x42,
    MinusLong = 0x43,
    PlusLong = 0x44,
    EnterLong = 0x46,
    PointLong = 0x47,
    DeleteLong = 0x48,
    
    // Additional button types for test compatibility
    PTT = 0x50,  // Push-to-talk
    MENU = 0x51,
    SELECT = 0x52,
    SCAN = 0x53
}