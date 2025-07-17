namespace BFormDomain.CommonCode.Platform;



/// <summary>
/// A UIErrorTemplate registers exceptions into an ExceptionFunnel. It describes how the exceptions translate into end-user-friendly messages.
/// </summary>
/// <param name="UserMessageTemplate">The end-user-friendly exception message.</param>
/// <param name="ExceptionType">The exception's type.</param>
/// <param name="ExceptionMessageKeys">Keywords found in the exceptions message. This is used to identify specific types of errors.</param>
/// <param name="RequireAllKeys">Determines if all keywords are required to funnel the exception.</param>
public readonly record struct UIErrorTemplate(
                           string UserMessageTemplate,
                           Type ExceptionType,
                           List<string> ExceptionMessageKeys,
                           bool RequireAllKeys = false);
