namespace BFormDomain.CommonCode.Notification;

public class WebNotification
{
    public string ForUserId { get; set; } = "";
    public WebNotificationKind Kind { get; set; }
    public string Info { get; set; } = "";
}
