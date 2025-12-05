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
    /// Enhanced pop frequencies.
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
    /// Optimized for cloassical music.
    /// </summary>
    Classical,

    /// <summary>
    /// Optimized for country music.
    /// </summary>
    Country
}
