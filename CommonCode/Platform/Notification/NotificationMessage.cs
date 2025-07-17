using Microsoft.Extensions.Logging;

namespace BFormDomain.CommonCode.Notification;

public class NotificationMessage
{

    

    public string Subject { get; set; } = "unknown";
    public string CreatorId { get; set; } = "unknown";

    public string? SMSText { get; set; }
    public string? EmailText { get; set; }
    public string? EmailHtmlText { get; set; }
    public string? ToastText { get; set; }

    public string? CallText { get; set; }

    public List<Guid> NotificationGroups { get; set; } = new();
    public Guid? NotificationGroup { get; set; }
    public Guid? NotificationContact { get; set; }

    public LogLevel Severity { get; set; }

    public bool WantDigest { get; set; } 
    public bool WantSuppression { get; set; }

    public int SuppressionMinutes { get; set; }
    public int DigestMinutes { get; set; }

    public int DigestHead { get; set; }
    public int DigestTail { get; set; }

    public NotificationMessage()
    {
        
        
    }

}


