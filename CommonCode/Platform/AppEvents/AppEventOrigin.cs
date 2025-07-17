namespace BFormDomain.CommonCode.Platform.AppEvents;


/// <summary>
/// CAG RE
/// </summary>
/// <param name="Generator"></param>
/// <param name="Action"></param>
/// <param name="Preceding"></param>
public record AppEventOrigin (string Generator, string? Action, AppEvent? Preceding);
