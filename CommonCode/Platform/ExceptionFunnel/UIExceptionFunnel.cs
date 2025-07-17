using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using Microsoft.Extensions.Logging;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform;


/// <summary>
/// The UIExceptionFunnel provides functions to translate
/// from obscure programmerese exception messages into something
/// end-users to will have a better time understanding. It also provides more information on what went wrong to the devs.
/// </summary>

public class UIExceptionFunnel
{
    private readonly MultiMap<Type,UIErrorTemplate> _templates = new();
    private readonly IApplicationTerms _appTerms;
    private readonly IApplicationAlert _alerts;

    /// <summary>
    /// DI Constructor. Register as transient.
    /// </summary>
    /// <param name="terms">Dependency Injected.</param>
    /// <param name="alerts">Dependency Injected.</param>
    public UIExceptionFunnel(
        IApplicationTerms terms,
        IApplicationAlert alerts)
    {
        _appTerms = terms;
        _alerts = alerts;
    }

    /// <summary>
    /// New UIErrorTemplates are registerd with this method.
    /// A template describes how a type of exception should be 
    /// mapped to end-user-friendly errors.
    /// </summary>
    /// <param name="templates"> Template Takes (End-ApplicationUser Message, Exception, Keywords found in exception) </param>
    public void Register(params UIErrorTemplate[] templates)
    {
        foreach (var template in templates)
            _templates.Add(template.ExceptionType, template);
    }


    /// <summary>
    /// Multple funnels can be registerd together here.
    /// A funnel is a collection of UIExceptionTemplates, which describe how exceptions are funneled into UI Errors. 
    /// The idea is that clients of this class can build reusable funnels that handle common groups of 
    /// exceptions in different contexts.
    /// a combined funnel:
    /// </summary>
    /// <param name="other"> The whole other funnel you want to combine with. </param>
    public void Register(UIExceptionFunnel other)
    {
        foreach (var key in other._templates.Keys)
        {
            var templateList = other._templates[key];
            foreach (var template in templateList.EmptyIfNull())
                _templates.Add(template.ExceptionType, template);
        }
    }
    /// <summary>
    /// GetAlertDetail uses the UIError reference code property to get the details (UserMessage, LineNumber, DevDetail) about the exception that was funneled.
    /// </summary>
    /// <param name="uiError">The UIError holds all of the information needed for a developer to understand what exception happened and in which file and line.</param>
    /// <returns>The dev detail level of information containing (the end-user-friendly message of the error, what line number this occured on, and the actual exception details.</returns>
    public static string GetAlertDetail(UIError uiError)
    {
        return $"{uiError.RefCode}: {uiError.Message}{Environment.NewLine}{uiError.DevDetail}";
    }

    /// <summary>
    /// This function:
    /// 1. Creates and returns a UIError end-user-friendly error object from the exception.
    /// 2. Records an application alert about the function
    /// </summary>
    /// <param name="ex">The funneled exception.</param>
    /// <param name="name">The name of the class that funnels the exception</param>
    /// <param name="limit">Limit raises an alerts warning level when (limit) amount of exceptions of the same type were funnelled</param>
    /// <returns>The error that was formatted for the end-user.</returns>
    public UIError FunnelAlert(Exception ex, string name, int limit = 1)
    {
        var uiError = Funnel(ex);
        var problem = GetAlertDetail(uiError);
        _alerts.RaiseAlert(ApplicationAlertKind.System, LogLevel.Warning,
            problem, limit, name);
        return uiError;
    }

    /// <summary>
    /// The CreateExceptionRefCode function creates a reference code that is presented both in the UIError and in the operational alert
    /// about the exception. The developer may use the ref code to lookup the operational alert reported by an end-user.
    /// </summary>
    /// <param name="ex">The exception used to create the ref code.</param>
    /// <returns>The reference code which is a hash code.</returns>
    private static int CreateExceptionRefCode(Exception ex)
    {
        return $"{DateTime.UtcNow} {ex.Message}".GetHashCode();
    }

    /// <summary>
    /// The funnel may not have a registered funnel for an error. 
    /// This method presents a default funneled error that will be used for any unregistered exception.
    /// It provides an error message that is generic, but end-user-friendly.
    /// </summary>
    /// <param name="ex">The exception to make default. It will provide other details if an alert gets raised. Eg: stack trace and ref code.</param>
    /// <returns>The default end-user-friendly error message.</returns>
    private UIError MakeDefault(Exception ex)
    {
        var message = _appTerms.ReplaceTerms("{ApplicationName} encountered a problem and couldn't process your request. Please retry, or contact your administrator");
        return new UIError(message, ex.TraceInformation(), CreateExceptionRefCode(ex));
    }

    /// <summary>
    /// The Funnel method:
    /// 1. Checks to see if the exception is in the funnel; 
    ///     1a. if the exception was found the exception, 
    ///             it replaces the exception text with a more readable version.
    ///     1b. if the funnel doesn't find the registed exception,
    ///             it calls MakeDefault to create a generic end-user-friendly UIError.
    /// </summary>
    /// <param name="ex">The exception to funnel.</param>
    /// <returns>The registered error translated into a end-user-friendly message.</returns>
    public UIError Funnel(Exception ex)
    {
        UIError result = default;
        var exceptionMessage = ex.Message.ToLowerInvariant();

        var matchCandidates = _templates[ex.GetType()];
        if(matchCandidates.EmptyIfNull().Any())
        {
            UIErrorTemplate? match = matchCandidates.EmptyIfNull().FirstOrDefault(it=>
                !it.ExceptionMessageKeys.Any() ||
                (it.RequireAllKeys ? 
                    it.ExceptionMessageKeys.All(emk=> exceptionMessage.Contains(emk.ToLowerInvariant())):
                    it.ExceptionMessageKeys.Any(emk=> exceptionMessage.Contains(emk.ToLowerInvariant())))
                    );

            if (match is not null)
            {
                var useTemplate = match.Value;
                var message = _appTerms.ReplaceTerms(useTemplate.UserMessageTemplate);
                result = new UIError(message, ex.TraceInformation(), CreateExceptionRefCode(ex));
            }
            else
                result = MakeDefault(ex);
        
        } else
        {
            result = MakeDefault(ex);
        }

        return result;
    }

}
