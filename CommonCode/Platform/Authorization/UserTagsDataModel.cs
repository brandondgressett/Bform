using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.DataModels;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Authorization;

public class UserTagsDataModel : IDataModel, ITaggable
{
    public Guid Id { get; set; }
    public int Version { get; set; }

    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public List<string> Tags { get; set; } = new List<string>();
}
