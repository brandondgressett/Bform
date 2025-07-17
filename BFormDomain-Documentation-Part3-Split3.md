# BFormDomain Part3 Documentation (Split 3)

#### RegulatedNotificationLogic
Core logic that handles message routing, regulation, and delivery:

```csharp
public class RegulatedNotificationLogic : IDisposable, IRegulatedNotificationLogic
{
    private readonly ILogger<RegulatedNotificationLogic> _logger;
    private readonly IRepository<NotificationGroup> _groupRepo;
    private readonly IRepository<NotificationContact> _contactRepo;
    private readonly IRepository<NotificationAudit> _auditRepo;
    private readonly INotificationCore _notificationCore;
    
    // Regulation components
    private readonly SuppressionOrder<Suppressible<ExecuteNotifyCommand>> _suppresser;
    private readonly SuppressionResultReceiver<Suppressible<ExecuteNotifyCommand>> _suppresserReceiver;
    private readonly ConsolidateToDigestOrder<Digestible<ExecuteNotifyCommand>> _digester;
    private readonly DigestResultReceiver<ExecuteNotifyCommand> _digestReceiver;
    
    public async Task Notify(NotificationMessage msg)
    {
        msg.Requires().IsNotNull();
        
        bool hasTarget = msg.NotificationGroups.Any() ||
                        msg.NotificationGroup.HasValue ||
                        msg.NotificationContact.HasValue;
        hasTarget.Requires().IsTrue();
        
        using (PerfTrack.Stopwatch("Regulated Notify"))
        {
            if (msg.NotificationGroups.Any())
                await GroupsNotify(msg);
            else if (msg.NotificationGroup.HasValue)
                await GroupNotify(msg, msg.NotificationGroup.Value);
            else if (msg.NotificationContact.HasValue)
                await ContactNotify(msg, msg.NotificationContact.Value);
        }
    }
    
    private async Task GroupNotify(NotificationMessage msg, Guid notificationGroupId)
    {
        var (group, _) = await _groupRepo.LoadAsync(notificationGroupId);
        group.Guarantees().IsNotNull();
        group!.Active.Guarantees().IsTrue();
        
        var (contacts, _) = await _contactRepo.LoadManyAsync(group.Members.Select(m => m.Id));
        if (contacts.Any())
        {
            var tasks = contacts
                .Select(contact => ContactNotify(msg, contact))
                .ToArray();
            
            await Task.WhenAll(tasks);
        }
    }
    
    private async Task ContactNotify(NotificationMessage msg, NotificationContact contact)
    {
        // Channel validation
        bool channelSelected = msg.SMSText is not null ||
                              msg.EmailText is not null ||
                              msg.EmailHtmlText is not null ||
                              msg.CallText is not null ||
                              msg.ToastText is not null;
        channelSelected.Requires().IsNotEqualTo(false);
        
        // Time and severity-based routing
        var sevContactRules = contact.TimeSeverityTable.BySeverity(msg.Severity);
        var tzi = TimeZoneInfo.FromSerializedString(contact.TimeZoneInfoId);
        var timeContactRules = sevContactRules.ByTime(tzi);
        
        DateTime digestUntilTime = DateTime.MinValue;
        if (msg.WantDigest)
            digestUntilTime = AcceptDigestParameters(msg, tzi);
        
        // Route to appropriate channels
        if (msg.SMSText is not null && !string.IsNullOrWhiteSpace(contact.TextNumber))
            await DoNotificationByRoute(msg, timeContactRules.Text, ChannelType.Text, contact, digestUntilTime, SendText);
        
        var finalEmailText = msg.EmailHtmlText ?? msg.EmailText;
        if (finalEmailText is not null && !string.IsNullOrWhiteSpace(contact.EmailAddress))
            await DoNotificationByRoute(msg, timeContactRules.Email, ChannelType.Email, contact, digestUntilTime, SendEmail);
        
        if (msg.CallText is not null && !string.IsNullOrWhiteSpace(contact.CallNumber))
            await DoNotificationByRoute(msg, timeContactRules.Call, ChannelType.Call, contact, digestUntilTime, SendCall);
        
        if (msg.ToastText is not null)
            await DoNotificationByRoute(msg, timeContactRules.Toast, ChannelType.Toast, contact, digestUntilTime, SendToast);
    }
    
    private async Task DoNotificationByRoute(
        NotificationMessage message,
        ChannelRegulation regulation,
        ChannelType channelType,
        NotificationContact nContact,
        DateTime nDigestUntilTime,
        Func<NotificationContact, NotificationMessage, bool, Task> action)
    {
        // Determine routing strategy
        ChannelRegulation route = ChannelRegulation.Allow;
        if (message.WantSuppression) route = ChannelRegulation.Suppress;
        if (message.WantDigest) route = ChannelRegulation.Digest;
        if (message.WantSuppression && message.WantDigest) route = ChannelRegulation.DigestSuppressed;
        
        if (regulation > route) route = regulation;
        
        var exec = new ExecuteNotifyCommand
        {
            NotificationMessage = message,
            Contact = nContact,
            ExecuteChannel = channelType
        };
        
        switch (route)
        {
            case ChannelRegulation.Allow:
                await action(nContact, message, true);
                break;
                
            case ChannelRegulation.Suppress:
                await _suppresser.MaybeSuppressAsync(
                    new Suppressible<ExecuteNotifyCommand>(
                        exec,
                        message.SuppressionMinutes,
                        message.Subject,
                        ne => ne.NotificationMessage!.Subject,
                        ne => ne.NotificationMessage!.CreatorId ?? "none",
                        ne => nContact.UserRef.ToString(),
                        ne => nContact.Id.ToString()));
                break;
                
            case ChannelRegulation.Digest:
            case ChannelRegulation.DigestSuppressed:
                if (route == ChannelRegulation.DigestSuppressed)
                    exec.DigestSuppressed = true;
                    
                await _digester.ConsolidateIntoDigestAsync(
                    new Digestible<ExecuteNotifyCommand>(
                        exec, nDigestUntilTime, message.DigestHead, message.DigestTail,
                        message.Subject,
                        ne => ne.NotificationMessage!.Subject,
                        ne => ne.NotificationMessage!.CreatorId ?? "none",
                        ne => nContact.UserRef.ToString(),
                        ne => nContact.Id.ToString()));
                break;
        }
    }
}
```

#### NotificationContact
Represents a notification recipient with channel preferences:

```csharp
public class NotificationContact : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // Contact information
    public Guid UserRef { get; set; }                       // Associated user
    public string ContactTitle { get; set; }                // Display name
    public bool Active { get; set; } = true;                // Contact status
    
    // Channel addresses
    public string? EmailAddress { get; set; }               // Email address
    public string? TextNumber { get; set; }                 // SMS phone number
    public string? CallNumber { get; set; }                 // Voice call number
    
    // Preferences
    public string TimeZoneInfoId { get; set; }              // Timezone for scheduling
    public NotificationTimeSeverityTable TimeSeverityTable { get; set; } // Channel rules
    
    // Metadata
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class NotificationTimeSeverityTable
{
    public Dictionary<LogLevel, NotificationChannelsAllowed> SeverityRules { get; set; } = new();
    
    public NotificationChannelsAllowed BySeverity(LogLevel severity)
    {
        return SeverityRules.TryGetValue(severity, out var rules) 
            ? rules 
            : NotificationChannelsAllowed.Default;
    }
}

public class NotificationChannelsAllowed
{
    public Dictionary<TimeShift, ChannelRegulation> TimeRules { get; set; } = new();
    
    public ChannelRegulation ByTime(TimeZoneInfo timeZone)
    {
        var currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var currentShift = TimeShifts.GetCurrentShift(currentTime);
        
        return TimeRules.TryGetValue(currentShift, out var regulation)
            ? regulation
            : ChannelRegulation.Allow;
    }
    
    public static NotificationChannelsAllowed Default => new()
    {
        TimeRules = new Dictionary<TimeShift, ChannelRegulation>
        {
            { TimeShift.BusinessHours, ChannelRegulation.Allow },
            { TimeShift.AfterHours, ChannelRegulation.Digest },
            { TimeShift.Weekend, ChannelRegulation.Digest }
        }
    };
}

public enum ChannelRegulation
{
    Allow = 0,           // Send immediately
    Suppress = 1,        // Suppress duplicates
    Digest = 2,          // Consolidate into digest
    DigestSuppressed = 3 // Suppress then digest
}
```

#### NotificationGroup
Manages groups of notification contacts:

```csharp
public class NotificationGroup : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // Group information
    public string Name { get; set; }                        // Group name
    public string? Description { get; set; }                // Group description
    public bool Active { get; set; } = true;                // Group status
    
    // Membership
    public List<NotificationContactReference> Members { get; set; } = new();
    
    // Metadata
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid CreatedBy { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class NotificationContactReference
{
    public Guid Id { get; set; }                            // Contact ID
    public string? Role { get; set; }                       // Member role
    public DateTime AddedDate { get; set; }                 // When added
    public bool Active { get; set; } = true;                // Member status
}
```

#### TwilioNotificationCore
Twilio-based implementation for SMS and voice notifications:

```csharp
public class TwilioNotificationCore : INotificationCore
{
    private readonly TwilioNotificationOptions _options;
    private readonly ILogger<TwilioNotificationCore> _logger;
    private readonly TwilioRestClient _twilioClient;
    
    public TwilioNotificationCore(
        IOptions<TwilioNotificationOptions> options,
        ILogger<TwilioNotificationCore> logger)
    {
        _options = options.Value;
        _logger = logger;
        _twilioClient = new TwilioRestClient(_options.AccountSid, _options.AuthToken);
    }
    
    public async Task SendText(string phoneNumber, string message)
    {
        try
        {
            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_options.FromPhoneNumber),
                to: new PhoneNumber(phoneNumber),
                client: _twilioClient);
            
            _logger.LogInformation("SMS sent successfully. SID: {MessageSid}", messageResource.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            throw;
        }
    }
    
    public async Task SendCall(string phoneNumber, string message)
    {
        try
        {
            var call = await CallResource.CreateAsync(
                url: new Uri(_options.VoiceCallbackUrl),
                to: new PhoneNumber(phoneNumber),
                from: new PhoneNumber(_options.FromPhoneNumber),
                client: _twilioClient);
            
            _logger.LogInformation("Voice call initiated successfully. SID: {CallSid}", call.Sid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate call to {PhoneNumber}", phoneNumber);
            throw;
        }
    }
    
    public async Task SendEmail(string emailAddress, string displayName, string subject, string? textBody, string? htmlBody)
    {
        // Delegate to email service (SendGrid, SMTP, etc.)
        await _emailService.SendEmailAsync(emailAddress, displayName, subject, textBody, htmlBody);
    }
    
    public async Task SendToast(Guid userId, string subject, string message)
    {
        // Create web notification for user
        var webNotification = new WebNotification
        {
            UserId = userId,
            Subject = subject,
            Message = message,
            NotificationType = WebNotificationKind.Toast,
            CreatedDate = DateTime.UtcNow,
            IsRead = false
        };
        
        await _webNotificationRepository.CreateAsync(webNotification);
        
        // Signal real-time notification if user is online
        await _webNotificationSink.NotifyUserAsync(userId, webNotification);
    }
}

public class TwilioNotificationOptions
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
    public string VoiceCallbackUrl { get; set; } = string.Empty;
}
```

#### WebNotification
In-app notification system:

```csharp
public class WebNotification : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    
    // Notification content
    public Guid UserId { get; set; }                        // Target user
    public string Subject { get; set; }                     // Notification title
    public string Message { get; set; }                     // Notification content
    public WebNotificationKind NotificationType { get; set; } // Notification type
    
    // State
    public bool IsRead { get; set; } = false;               // Read status
    public DateTime? ReadDate { get; set; }                 // When read
    public DateTime CreatedDate { get; set; }               // Creation time
    public DateTime? ExpiryDate { get; set; }               // Optional expiry
    
    // Metadata
    public string? ActionUrl { get; set; }                  // Optional action link
    public JObject? Data { get; set; }                      // Additional data
    public int Priority { get; set; } = 0;                  // Display priority
}

public enum WebNotificationKind
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Toast = 4,
    Alert = 5
}

public interface IWebNotificationSink
{
    Task NotifyUserAsync(Guid userId, WebNotification notification);
    Task MarkAsReadAsync(Guid userId, Guid notificationId);
    Task<List<WebNotification>> GetUnreadNotificationsAsync(Guid userId);
}
```

### Message Regulation Features

#### Duplicate Suppression
Prevents sending duplicate messages within a specified time window:

```csharp
// Enable suppression for 30 minutes
var message = new NotificationMessage
{
    Subject = "System Alert",
    EmailText = "Database connection lost",
    WantSuppression = true,
    SuppressionMinutes = 30,
    NotificationGroup = alertGroupId
};
```

#### Digest Consolidation
Consolidates multiple messages into a single digest:

```csharp
// Enable digest consolidation for 60 minutes
var message = new NotificationMessage
{
    Subject = "Order Updates",
    EmailText = "Order #12345 has been shipped",
    WantDigest = true,
    DigestMinutes = 60,
    DigestHead = 5,     // Show first 5 messages
    DigestTail = 5,     // Show last 5 messages
    NotificationGroup = ordersGroupId
};
```

#### Combined Regulation
Suppress duplicates and then consolidate into digests:

```csharp
var message = new NotificationMessage
{
    Subject = "Performance Alert",
    EmailText = "CPU usage above 90%",
    WantSuppression = true,
    WantDigest = true,
    SuppressionMinutes = 15,
    DigestMinutes = 60,
    NotificationGroup = opsTeamId
};
```

### Usage Examples

#### Basic Notification Sending
```csharp
public class NotificationExamples
{
    private readonly IMessagePublisher _messagePublisher;
    
    // Send immediate notification
    public async Task SendWelcomeEmail(Guid userId)
    {
        var message = new NotificationMessage
        {
            Subject = "Welcome to Our Platform!",
            EmailText = "Thank you for joining us. Here's how to get started...",
            EmailHtmlText = "<h1>Welcome!</h1><p>Thank you for joining us...</p>",
            NotificationContact = userId,
            Severity = LogLevel.Information
        };
        
        await _messagePublisher.SendAsync(message, NotificationService.RouteName);
    }
    
    // Send multi-channel alert
    public async Task SendSecurityAlert(Guid securityGroupId, string alertDetails)
    {
        var message = new NotificationMessage
        {
            Subject = "SECURITY ALERT",
            EmailText = $"Security incident detected: {alertDetails}",
            SMSText = $"SECURITY ALERT: {alertDetails}",
            ToastText = $"Security Alert: {alertDetails}",
            NotificationGroup = securityGroupId,
            Severity = LogLevel.Critical,
            WantSuppression = true,
            SuppressionMinutes = 5 // Prevent spam during incidents
        };
        
        await _messagePublisher.SendAsync(message, NotificationService.RouteName);
    }
    
    // Send digest-enabled notifications
    public async Task SendOrderNotification(Guid customerId, string orderDetails)
    {
        var message = new NotificationMessage
        {
            Subject = "Order Update",
            EmailText = $"Your order has been updated: {orderDetails}",
            ToastText = $"Order update: {orderDetails}",
            NotificationContact = customerId,
            Severity = LogLevel.Information,
            WantDigest = true,
            DigestMinutes = 120, // 2-hour digest window
            DigestHead = 3,
            DigestTail = 2
        };
        
        await _messagePublisher.SendAsync(message, NotificationService.RouteName);
    }
}
```

#### Rule Action Integration
```csharp
public class RuleActionRequestNotification : IRuleAction
{
    private readonly IMessagePublisher _messagePublisher;
    
    public async Task<object> ExecuteAsync(
        Dictionary<string, object?> args,
        AppEvent appEvent,
        RuleEvaluationContext context)
    {
        var groupByTags = args.GetValueOrDefault("GroupByTags") as string[];
        var subject = args.GetValueOrDefault("Subject") as string;
        var emailText = args.GetValueOrDefault("EmailText") as string;
        var smsText = args.GetValueOrDefault("SMSText") as string;
        var toastText = args.GetValueOrDefault("ToastText") as string;
        var severity = Enum.Parse<LogLevel>(args.GetValueOrDefault("Severity") as string ?? "Information");
        
        // Find notification groups by tags
        var groups = await FindGroupsByTags(groupByTags);
        
        var message = new NotificationMessage
        {
            Subject = subject,
            EmailText = emailText,
            SMSText = smsText,
            ToastText = toastText,
            NotificationGroups = groups.Select(g => g.Id).ToList(),
            Severity = severity,
            CreatorId = appEvent.OriginId?.ToString() ?? "system"
        };
        
        await _messagePublisher.SendAsync(message, NotificationService.RouteName);
        
        return new { sent = true, groupCount = groups.Count };
    }
}
```

#### Advanced Routing with Time-Based Rules
```csharp
public class NotificationContactService
{
    public async Task SetupManagerContact(Guid userId, Guid managerId)
    {
        var contact = new NotificationContact
        {
            UserRef = userId,
            ContactTitle = "John Manager",
            EmailAddress = "john.manager@company.com",
            TextNumber = "+1234567890",
            CallNumber = "+1234567890",
            TimeZoneInfoId = TimeZoneInfo.Local.ToSerializedString(),
            
            // Configure time and severity-based routing
            TimeSeverityTable = new NotificationTimeSeverityTable
            {
                SeverityRules = new Dictionary<LogLevel, NotificationChannelsAllowed>
                {
                    // Critical: Always allow all channels
                    [LogLevel.Critical] = new NotificationChannelsAllowed
                    {
                        TimeRules = new Dictionary<TimeShift, ChannelRegulation>
                        {
                            { TimeShift.BusinessHours, ChannelRegulation.Allow },
                            { TimeShift.AfterHours, ChannelRegulation.Allow },
                            { TimeShift.Weekend, ChannelRegulation.Allow }
                        }
                    },
                    
                    // Warning: Business hours immediate, otherwise digest
                    [LogLevel.Warning] = new NotificationChannelsAllowed
                    {
                        TimeRules = new Dictionary<TimeShift, ChannelRegulation>
                        {
                            { TimeShift.BusinessHours, ChannelRegulation.Allow },
                            { TimeShift.AfterHours, ChannelRegulation.Digest },
                            { TimeShift.Weekend, ChannelRegulation.Digest }
                        }
                    },
                    
                    // Info: Always digest outside business hours
                    [LogLevel.Information] = new NotificationChannelsAllowed
                    {
                        TimeRules = new Dictionary<TimeShift, ChannelRegulation>
                        {
                            { TimeShift.BusinessHours, ChannelRegulation.Allow },
                            { TimeShift.AfterHours, ChannelRegulation.Digest },
                            { TimeShift.Weekend, ChannelRegulation.Suppress }
                        }
                    }
                }
            }
        };
        
        await _contactRepository.CreateAsync(contact);
    }
}
```

### Best Practices

1. **Use appropriate severity levels** - Match LogLevel to message importance
2. **Enable regulation wisely** - Use suppression for alerts, digests for updates
3. **Configure time-based rules** - Respect recipient preferences and schedules
4. **Provide multiple channels** - Allow fallback options for critical messages
5. **Monitor delivery rates** - Track success/failure metrics
6. **Maintain contact hygiene** - Keep contact information current
7. **Use meaningful subjects** - Help recipients prioritize messages
8. **Test notification flows** - Verify delivery across all channels
9. **Handle failures gracefully** - Implement retry logic and fallbacks
10. **Audit notification activity** - Maintain compliance and debugging trails

---
