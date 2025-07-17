using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.Platform.Tags;


public class TagCountsDataModel : IDataModel
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }

    public string Tag { get; set; } = "";

    public string? EntityType { get; set; } = null!;
    public string? TemplateType { get; set; } = null!;

    public int Count { get; set; } = 0;

}
