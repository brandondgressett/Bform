namespace BFormDomain.CommonCode.Platform;



/// <summary>
/// Represents an error suitable to present to end-users.
/// </summary>
/// <param name="Message">An end-user-friendly version of the exception message.</param>
/// <param name="DevDetail">DevDetail provides developer-friendly details about the exception. Eg, stack trace, exception message.</param>
/// <param name="RefCode">The DevDetail and operational alert refer to the same unique ref code. The ref code provides a way to correlate the Message to the alert.</param>
public readonly record struct UIError(string Message, string DevDetail, int RefCode);
