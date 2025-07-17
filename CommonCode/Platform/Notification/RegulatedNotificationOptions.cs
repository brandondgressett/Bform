namespace BFormDomain.CommonCode.Notification;

public class RegulatedNotificationOptions
{
    public int GroupNotFoundErrorThreshold { get; set; } = 15;
    public int NotificationContactNotFoundErrorThreshold { get; set; } = 50;

    public int SendNotificationErrorThreshold { get; set; } = 150;
    public int DefaultDigestHeadLength { get; set; } = 100;
    public int DefaultDigestTailLength { get; set; } = 10;

    public int MaxEmailDigestItems { get; set; } = 200;
    
}

