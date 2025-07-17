using BFormDomain.CommonCode.Notification;
using BFormDomain.HelperClasses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace BFormDomain.Diagnostics;

public class SwitchingApplicationAlert : IApplicationAlert
{
    private readonly ILogger<SwitchingApplicationAlert> _logger;
    private readonly RequestNotification _requestNotification;
    private readonly BuiltInNotificationGroups _builtInNotificationGroups;
    private readonly bool _call;
    private readonly bool _text;
    private readonly bool _email;

    public SwitchingApplicationAlert(
        ILogger<SwitchingApplicationAlert> logger, RequestNotification requestNotification,
        BuiltInNotificationGroups builtInNotificationGroups,
        IOptions<SwitchingApplicationAlertOptions> options)
    {
        _logger = logger;
        _requestNotification = requestNotification;
        _builtInNotificationGroups = builtInNotificationGroups;
        var optVal = options.Value;
        _call = optVal.Call;  
        _text = optVal.Text;
        _email = optVal.Email;

        if (!_call && !_text && !_email)
            _email = true;
    }

    private static readonly ConcurrentDictionary<string, int> _alertCounts = new ();
    private static DateTime _countExpiration = DateTime.MinValue;
    private static readonly object _clearLock = new ();


    static SwitchingApplicationAlert()
    {
        _countExpiration = DateTime.UtcNow.AddMinutes(15.0);
    }
    

    private static void MaybeClearCounts()
    {
        lock(_clearLock)
        {
            if(DateTime.UtcNow > _countExpiration)
            {
                _countExpiration = DateTime.UtcNow.AddMinutes(15.0);
                _alertCounts.Clear();
            }
        }
    }

    public void RaiseAlert(
        ApplicationAlertKind kind, LogLevel level,
        string details,
        int limitIn15 = 0,
        string limitGroup="", 
        [CallerFilePath] string file = "unknown", 
        [CallerLineNumber] int line = -1, 
        [CallerMemberName] string member = "unknown")
    {
        MaybeClearCounts();
        bool doNotify = false;

        if (limitIn15 > 0)
        {
            if (string.IsNullOrEmpty(limitGroup))
                limitGroup = $"{file} {line} {member}";

            int currentCount = _alertCounts.AddOrUpdate(limitGroup, 1, (k, v) => v + 1);
            if(currentCount > limitIn15)
            {
                doNotify = true;
                _alertCounts.AddOrUpdate(limitGroup, 0, (k, v) => 0);
            }

        }

        var sb = new StringBuilder();
        sb.AppendLine($"{limitGroup ?? ""} {DateTime.Now} {kind.EnumName()} {level.EnumName()} {file} {line} {member}:");
        sb.AppendLine(details);

        switch (level)
        {
            case LogLevel.Debug:
            case LogLevel.Information:
            case LogLevel.Warning:
                _logger.LogInformation(message: sb.ToString());
                break;

            case LogLevel.Critical:
            case LogLevel.Error:
                _logger.LogCritical(message: sb.ToString());
                doNotify = true;
                break;

            default:
                break;
        }

        if (doNotify)
        {
            
            try
            {

                var groupId = _builtInNotificationGroups.ApplicationAlertsGroupId;

                var shortBody = $"Application alert from {file} {line} {member}. Severity: {level}.";
                var longBody = shortBody + $" {sb}";

                AsyncHelper.RunSync(() =>
                    _requestNotification.Request(
                        new NotificationMessage
                        {
                            CallText = _call ? shortBody : null,
                            CreatorId = "Alert Notification",
                            DigestHead = 10,
                            DigestTail = 10,
                            DigestMinutes = 0,
                            EmailText = _email ? longBody : null,
                            NotificationGroup = groupId,
                            Severity = level,
                            SMSText = _text ? shortBody : null,
                            Subject = $"Alert Notification {member ?? file + ":" + line}",
                            SuppressionMinutes = 60,
                            WantDigest = true,
                            WantSuppression = true

                        }));

            } catch
            {

            }
        }


    }

    public void RaiseAlert(ApplicationAlertKind general, LogLevel information, object p)
    {
        throw new NotImplementedException();
    }
}
