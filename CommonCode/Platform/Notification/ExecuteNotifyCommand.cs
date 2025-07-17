namespace BFormDomain.CommonCode.Notification;

public class ExecuteNotifyCommand
{

    public NotificationMessage? NotificationMessage { get; set; }
    public ChannelType ExecuteChannel { get; set; }

    public bool DigestSuppressed { get; set; }

    public NotificationContact? Contact { get; set; }
    
}


