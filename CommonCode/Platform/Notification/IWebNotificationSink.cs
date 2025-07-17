namespace BFormDomain.CommonCode.Notification;

public interface IWebNotificationSink
{
    ValueTask PushAsync(WebNotification notification);
}