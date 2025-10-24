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
    public override MessageType MessageType => MessageType.BUTTON_PRESS;
    
    public ButtonPressMessage(ButtonType buttonType, int radioId = ProtocolConstants.DEFAULT_RADIO_ID) 
        : base(radioId)
    {
        ButtonType = buttonType;
    }
}
