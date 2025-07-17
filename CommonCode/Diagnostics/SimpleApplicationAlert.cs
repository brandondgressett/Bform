using BFormDomain.HelperClasses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using BFormDomain.Diagnostics;
namespace BFormDomain.Diagnostics;


public class SimpleApplicationAlert : IApplicationAlert
{
    private readonly ILogger<SimpleApplicationAlert> _logger;


    public SimpleApplicationAlert(
        ILogger<SimpleApplicationAlert> logger)
    {
        _logger = logger;

    }

    private static readonly ConcurrentDictionary<string, int> _alertCounts = new();
    private static DateTime _countExpiration = DateTime.MinValue;
    private static readonly object _clearLock = new();


    static SimpleApplicationAlert()
    {
        _countExpiration = DateTime.UtcNow.AddMinutes(15.0);
    }


    private static void MaybeClearCounts()
    {
        lock (_clearLock)
        {
            if (DateTime.UtcNow > _countExpiration)
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
        string limitGroup = "",
        [CallerFilePath] string file = "unknown",
        [CallerLineNumber] int line = -1,
        [CallerMemberName] string member = "unknown")
    {
        MaybeClearCounts();


        if (limitIn15 > 0)
        {
            if (string.IsNullOrEmpty(limitGroup))
                limitGroup = $"{file} {line} {member}";

            int currentCount = _alertCounts.AddOrUpdate(limitGroup, 1, (k, v) => v + 1);
            if (currentCount > limitIn15)
            {

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
               
                break;

            default:
                break;
        }

    }

    public void RaiseAlert(ApplicationAlertKind general, LogLevel information, object p)
    {
        throw new NotImplementedException();
    }
}