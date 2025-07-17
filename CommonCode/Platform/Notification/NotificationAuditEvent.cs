using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Notification;

public class NotificationAuditEvent
{
    public DateTime DateTime { get; set; }
    public ChannelType ChannelType { get; set; }
    public string Subject { get; set; } = "none";
    public string Body { get; set; } = "none";
    public LogLevel Severity { get; set; }
}
