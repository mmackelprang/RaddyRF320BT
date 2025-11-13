using System;
using System.Collections.Generic;

namespace RadioProtocol.Core.Protocol;

/// <summary>
/// Canonical actions that can be performed on the radio
/// </summary>
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

/// <summary>
/// Maps canonical actions to command IDs
/// </summary>
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

/// <summary>
/// Factory for building protocol frames
/// </summary>
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
