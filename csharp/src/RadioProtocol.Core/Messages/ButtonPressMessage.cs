using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Messages;

/// <summary>
/// Button press message
/// </summary>
public record ButtonPressMessage : BaseMessage
{
    /// <summary>
    /// Button type being pressed
    /// </summary>
    public ButtonType ButtonType { get; init; }
    
    /// <summary>
    /// Message type for button press
    /// </summary>
    public override MessageType MessageType => MessageType.ButtonPress;
    
    public ButtonPressMessage(ButtonType buttonType, int radioId = ProtocolConstants.DefaultRadioId) 
        : base(radioId)
    {
        ButtonType = buttonType;
    }
}
