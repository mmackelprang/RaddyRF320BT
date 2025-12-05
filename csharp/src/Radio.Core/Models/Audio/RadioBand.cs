namespace Radio.Core.Models.Audio;

/// <summary>
/// Represents the radio frequency band.
/// </summary>
public enum RadioBand
{
  /// <summary>
  /// Amplitude Modulation band (470-1760 kHz).
  /// </summary>
  AM,

  /// <summary>
  /// Frequency Modulation band (86.1-108.9 MHz).
  /// </summary>
  FM,

  /// <summary>
  /// Weather Band for NOAA weather radio (162.400-162.550 MHz).
  /// </summary>
  WB,

  /// <summary>
  /// Very High Frequency band (30-199 MHz).
  /// </summary>
  VHF,

  /// <summary>
  /// Shortwave band (3.16-4.14 MHz).
  /// </summary>
  SW,

  /// <summary>
  /// Airplane band (118-138 MHz).
  /// </summary>
  AIR
}
