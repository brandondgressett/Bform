namespace BFormDomain.CommonCode.Notification;

public class ChannelsAllowed
{
    public ChannelRegulation Email { get; set; }
    public ChannelRegulation Text { get; set; }
    public ChannelRegulation Call { get; set; }

    public ChannelRegulation Toast { get; set; }
}
