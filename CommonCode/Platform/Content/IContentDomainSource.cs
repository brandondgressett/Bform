namespace BFormDomain.CommonCode.Platform.Content;

/// <summary>
/// Each entity with content should provide an implementation of this interface,
/// and then register each one as a service in DI.
/// Then the IApplicationPlatformContent implementation can inject
/// IEnumerable<IContentDomainSource> to know about them all.
/// </summary>
public interface IContentDomainSource
{
    ContentDomain Tell(IApplicationPlatformContent host);


}
