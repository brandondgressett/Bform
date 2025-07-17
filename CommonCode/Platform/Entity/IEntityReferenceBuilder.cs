namespace BFormDomain.CommonCode.Platform.Entity;

/// <summary>
/// Key dependency injection on EntityType
/// </summary>
public interface IEntityReferenceBuilder
{
    Uri MakeReference(string templateName, Guid id, bool template = false, bool vm = false, string? queryParameters=null);
}
