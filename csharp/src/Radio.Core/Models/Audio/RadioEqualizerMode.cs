namespace Radio.Core.Models.Audio;

/// <summary>
/// Represents the equalizer mode for the radio device audio output.
/// </summary>
public enum RadioEqualizerMode
{
    /// <summary>
    /// Normal/flat audio output with no equalization.
    /// </summary>
    Normal,

    /// <summary>
    /// Enhanced bass frequencies.
    /// </summary>
    Bass,

    /// <summary>
    /// Optimized for popular music.
    /// </summary>
    Pop,

    /// <summary>
    /// Optimized for rock music.
    /// </summary>
    Rock,

    /// <summary>
    /// Optimized for jazz music.
    /// </summary>
    Jazz,

    /// <summary>
    /// Optimized for classical music.
    /// </summary>
    Classical
}
