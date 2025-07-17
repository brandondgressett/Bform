namespace BFormDomain.CommonCode.Notification;

public class TwilioNotificationOptions
{
    public string SendGridKey { get; set; } = null!;
    public string TwilioSid { get; set; } = null!;
    public string TwilioToken { get; set; } = null!;

    public string FromEmail { get; set; } = null!;
    public string FromEmailName { get; set; } = null!;
    public string FromPhoneNumber { get; set; } = null!;

    public int FailureThreshold { get; set; } = 50;
}
