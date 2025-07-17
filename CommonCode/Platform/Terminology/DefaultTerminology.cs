using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Platform.Terminology;

internal static class DefaultTerminology
{
    private static readonly IDictionary<string, string> DefaultTerms =
    new ConcurrentDictionary<string, string>
    {
        ["AppName"] = "BForm",
        ["InvitationSubject"] = "Register on BForm",
        ["InvitationBody"] = "Invitation Code: {invitecode}",
        ["PasswordResetSubject"] = "Password Reset",
        ["PasswordResetBody"]  = "Your new BForm password: {password}",
        ["Project"] = "Project",
        ["Projects"] = "Projects",
        ["SignUpGreeting"] = "<!-- BForm HTML --><h2>Welcome to <b>BForm</b>.</h2>",
        ["Ticket"] = "Ticket",
        ["Tickets"] = "Tickets"
        
    };

    public static IReadOnlyDictionary<string, string> Terms 
    { 
        get 
        { 
            return (IReadOnlyDictionary<string, string>) DefaultTerms; 
        } 
    }


}
