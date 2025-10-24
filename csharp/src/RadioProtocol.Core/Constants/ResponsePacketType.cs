namespace RadioProtocol.Core.Constants;

/// <summary>
/// Response packet types from radio
/// </summary>
public enum ResponsePacketType
{
    FrequencyStatus,      // ab0417
    Time,                 // ab031e  
    BandInfo,            // ab0901
    Volume,              // ab0303
    Signal,              // ab031f
    FrequencyInput,      // ab090f
    DeviceInfo,          // ab11
    DeviceInfoCont,      // ab10
    SubBandInfo,         // ab0e
    LockStatus,          // ab08
    RecordingStatus,     // ab0b
    StatusShort,         // ab02
    FreqData1,           // ab05
    FreqData2,           // ab06
    Battery,             // ab07
    DetailedFreq,        // ab09
    Bandwidth,           // ab0d
    Unknown
}