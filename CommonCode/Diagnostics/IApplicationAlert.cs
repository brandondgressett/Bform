using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace BFormDomain.Diagnostics;


public interface IApplicationAlert
{
    void RaiseAlert(ApplicationAlertKind kind, LogLevel severity,
        string details,
        int limitIn15 = 0,
        string limitGroup = "",  
        [CallerFilePath] string file = "unknown",
        [CallerLineNumber] int line=-1, 
        [CallerMemberName] string member="unknown");
    void RaiseAlert(ApplicationAlertKind general, LogLevel information, object p);
}


