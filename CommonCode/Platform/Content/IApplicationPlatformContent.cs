using Newtonsoft.Json.Schema;

namespace BFormDomain.CommonCode.Platform.Content;

public interface IApplicationPlatformContent
{
    JSchema LoadEmbeddedSchema<T>() where T: IContentType;

    IEnumerable<ContentDomain> Domains { get; }

    IList<T> GetAllContent<T>() where T : IContentType;
    T? GetContentByName<T>(string name) where T : IContentType;

    ContentElement? ViewContentType(string name);

    IList<T> GetMatchingAny<T>(params string[] tags) where T : IContentType;
    IList<T> GetMatchingAll<T>(params string[] tags) where T : IContentType;

    string? GetFreeJson(string name);


}
