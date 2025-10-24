using RadioProtocol.Core.Constants;

namespace RadioProtocol.Core.Messages;

/// <summary>
/// Channel command message
/// </summary>
public record ChannelCommandMessage : BaseMessage
{
    /// <summary>
    /// Channel number (0-255)
    /// </summary>
    public int ChannelNumber { get; init; }
    
    /// <summary>
    /// Message type for channel command
    /// </summary>
    public override MessageType MessageType => MessageType.ChannelCommand;
    
    public ChannelCommandMessage(int channelNumber, int radioId = ProtocolConstants.DefaultRadioId) 
        : base(radioId)
    {
        if (channelNumber < 0 || channelNumber > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(channelNumber), 
                "Channel number must be between 0 and 255");
        }
        
        ChannelNumber = channelNumber;
    }
}
