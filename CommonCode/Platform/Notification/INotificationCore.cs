namespace BFormDomain.CommonCode.Notification;

public interface INotificationCore
{
    Task SendCall(string callPhoneNumber, string sayThis);
    Task SendEmail(string emailAddress, string emailName, string subject, string? plainTextContent = null, string? htmlContent = null);
    Task SendText(string textPhoneNumber, string textBody);

    Task SendToast(Guid userId, string subject, string details);
}
