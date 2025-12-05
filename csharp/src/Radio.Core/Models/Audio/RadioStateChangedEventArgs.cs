namespace Radio.Core.Models.Audio;

/// <summary>
/// Provides data for the <see cref="Radio.Core.Interfaces.Audio.IRadioControls.StateChanged"/> event.
/// </summary>
public class RadioStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the name of the property that changed.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the previous value of the property, if available.
    /// </summary>
    public object? OldValue { get; }

    /// <summary>
    /// Gets the new value of the property.
    /// </summary>
    public object? NewValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RadioStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    /// <param name="oldValue">The previous value of the property.</param>
    /// <param name="newValue">The new value of the property.</param>
    public RadioStateChangedEventArgs(string propertyName, object? oldValue = null, object? newValue = null)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
