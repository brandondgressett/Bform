
namespace BFormDomain.CommonCode.Notification;

public interface IRegulatedNotificationLogic
{
    void Initialize();
    Task Notify(NotificationMessage msg);
}
