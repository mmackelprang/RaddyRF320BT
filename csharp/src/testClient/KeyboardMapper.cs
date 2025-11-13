using System;
using System.Collections.Generic;

namespace RadioClient;

public static class KeyboardMapper
{
    private static readonly Dictionary<ConsoleKey, CanonicalAction> _keyMap = new()
    {
        // Numbers
        { ConsoleKey.D1, CanonicalAction.Number1 },
        { ConsoleKey.D2, CanonicalAction.Number2 },
        { ConsoleKey.D3, CanonicalAction.Number3 },
        { ConsoleKey.D4, CanonicalAction.Number4 },
        { ConsoleKey.D5, CanonicalAction.Number5 },
        { ConsoleKey.D6, CanonicalAction.Number6 },
        { ConsoleKey.D7, CanonicalAction.Number7 },
        { ConsoleKey.D8, CanonicalAction.Number8 },
        { ConsoleKey.D9, CanonicalAction.Number9 },
        { ConsoleKey.D0, CanonicalAction.Number0 },

        // NumPad numbers
        { ConsoleKey.NumPad1, CanonicalAction.Number1 },
        { ConsoleKey.NumPad2, CanonicalAction.Number2 },
        { ConsoleKey.NumPad3, CanonicalAction.Number3 },
        { ConsoleKey.NumPad4, CanonicalAction.Number4 },
        { ConsoleKey.NumPad5, CanonicalAction.Number5 },
        { ConsoleKey.NumPad6, CanonicalAction.Number6 },
        { ConsoleKey.NumPad7, CanonicalAction.Number7 },
        { ConsoleKey.NumPad8, CanonicalAction.Number8 },
        { ConsoleKey.NumPad9, CanonicalAction.Number9 },
        { ConsoleKey.NumPad0, CanonicalAction.Number0 },

        // Navigation
        { ConsoleKey.UpArrow, CanonicalAction.UpShort },
        { ConsoleKey.DownArrow, CanonicalAction.DownShort },
        
        // Volume
        { ConsoleKey.Add, CanonicalAction.VolAdd },          // + key
        { ConsoleKey.OemPlus, CanonicalAction.VolAdd },      // = key (with shift)
        { ConsoleKey.Subtract, CanonicalAction.VolDel },     // - key
        { ConsoleKey.OemMinus, CanonicalAction.VolDel },     // - key

        // Decimal/Point
        { ConsoleKey.OemPeriod, CanonicalAction.Point },     // . key
        { ConsoleKey.Decimal, CanonicalAction.Point },       // NumPad decimal

        // Function keys
        { ConsoleKey.B, CanonicalAction.Band },
        { ConsoleKey.P, CanonicalAction.Power },
        { ConsoleKey.M, CanonicalAction.Music },
        { ConsoleKey.S, CanonicalAction.Step },
        { ConsoleKey.T, CanonicalAction.SubBand },           // T for sub-band (alternative Band)
        { ConsoleKey.L, CanonicalAction.Play },              // pLay
        { ConsoleKey.C, CanonicalAction.Circle },
        { ConsoleKey.Q, CanonicalAction.SQ },                // SQ (squelch)
        { ConsoleKey.R, CanonicalAction.Rec },               // Record
        { ConsoleKey.D, CanonicalAction.Demodulation },
        { ConsoleKey.W, CanonicalAction.BandWidth },         // Width
        { ConsoleKey.O, CanonicalAction.MobileDisplay },     // mOde display
        { ConsoleKey.E, CanonicalAction.Stereo },            // stEreo
        { ConsoleKey.X, CanonicalAction.Preset },            // preset (save)
        { ConsoleKey.N, CanonicalAction.Memo },              // memo (Note)
        { ConsoleKey.Y, CanonicalAction.DeEmphasis },        // de-emphasis
        { ConsoleKey.U, CanonicalAction.Bluetooth },         // Bluetooth

        // Special functions
        { ConsoleKey.Backspace, CanonicalAction.Back },
        { ConsoleKey.Enter, CanonicalAction.FreqConfirm },
        { ConsoleKey.Spacebar, CanonicalAction.MusicCycle },
    };

    private static readonly Dictionary<(ConsoleKey, ConsoleModifiers), CanonicalAction> _modifierKeyMap = new()
    {
        // Long presses (Shift modifier simulates long press)
        { (ConsoleKey.UpArrow, ConsoleModifiers.Shift), CanonicalAction.UpLong },
        { (ConsoleKey.DownArrow, ConsoleModifiers.Shift), CanonicalAction.DownLong },
        { (ConsoleKey.L, ConsoleModifiers.Shift), CanonicalAction.LongPlay },
        { (ConsoleKey.B, ConsoleModifiers.Shift), CanonicalAction.BandLong },

        // Hold actions (Ctrl modifier)
        { (ConsoleKey.D1, ConsoleModifiers.Control), CanonicalAction.Num1Hold },
        { (ConsoleKey.D2, ConsoleModifiers.Control), CanonicalAction.Num2Hold },
        { (ConsoleKey.D3, ConsoleModifiers.Control), CanonicalAction.Num3Hold },
        { (ConsoleKey.D4, ConsoleModifiers.Control), CanonicalAction.Num4Hold },
        { (ConsoleKey.D5, ConsoleModifiers.Control), CanonicalAction.Num5Hold },
        { (ConsoleKey.D6, ConsoleModifiers.Control), CanonicalAction.Num6Hold },
        { (ConsoleKey.D7, ConsoleModifiers.Control), CanonicalAction.Num7Hold },
        { (ConsoleKey.D8, ConsoleModifiers.Control), CanonicalAction.Num8Hold },
        { (ConsoleKey.D9, ConsoleModifiers.Control), CanonicalAction.Num9Hold },
        { (ConsoleKey.D0, ConsoleModifiers.Control), CanonicalAction.Num0Hold },
        { (ConsoleKey.P, ConsoleModifiers.Control), CanonicalAction.PowerHold },
        { (ConsoleKey.M, ConsoleModifiers.Control), CanonicalAction.MusicHold },
        { (ConsoleKey.L, ConsoleModifiers.Control), CanonicalAction.PlayHold },
        { (ConsoleKey.OemPeriod, ConsoleModifiers.Control), CanonicalAction.PointHold },
        { (ConsoleKey.N, ConsoleModifiers.Control), CanonicalAction.MemoHold },
    };

    public static bool TryGetAction(ConsoleKeyInfo keyInfo, out CanonicalAction action)
    {
        action = default;

        // Check for modifier combinations first
        if (keyInfo.Modifiers != 0)
        {
            if (_modifierKeyMap.TryGetValue((keyInfo.Key, keyInfo.Modifiers), out action))
            {
                return true;
            }
        }

        // Check for simple key mappings
        if (_keyMap.TryGetValue(keyInfo.Key, out action))
        {
            return true;
        }

        return false;
    }

    public static string GetKeyboardHelp()
    {
        return @"
┌──────────────────────────────────────────────────────────────────────────┐
│                    RF320 Radio Test Client - Keyboard Map                │
├──────────────────────────────────────────────────────────────────────────┤
│  NUMBERS:  0-9 = Number keys      │  NAVIGATION: ↑/↓ = Up/Down Short    │
│  VOLUME:   +   = Volume Up         │              Shift+↑/↓ = Up/Dn Long │
│            -   = Volume Down       │  SPECIAL:    . = Decimal Point      │
│                                    │              ⏎ = Freq Confirm       │
│  FUNCTIONS:                        │              ⌫ = Back               │
│    B = Band         M = Music      │              ␣ = Music Cycle        │
│    P = Power        L = Play       │              Esc = EXIT PROGRAM     │
│    S = Step         C = Circle     │                                     │
│    T = Sub-Band     Q = SQ         │  HOLDS (Ctrl+Key):                  │
│    R = Record       D = Demod      │    Ctrl+0-9 = Number Hold           │
│    W = BandWidth    O = Display    │    Ctrl+P = Power Hold              │
│    E = Stereo       Y = DeEmph     │    Ctrl+M = Music Hold              │
│    X = Preset       N = Memo       │    Ctrl+. = Point Hold              │
│    U = Bluetooth                   │                                     │
└──────────────────────────────────────────────────────────────────────────┘
";
    }
}
