using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.Notification;

public class NotificationGroup : IDataModel, ITaggable
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }

    public List<NotificationContactReference> Members { get; set; } = new();

    public string GroupTitle { get; set; } = "";

    public string GroupDescription { get; set; } = "";

    public List<string> Tags { get; set; } = new();


    public bool Active { get; set; } = true;

    
}
