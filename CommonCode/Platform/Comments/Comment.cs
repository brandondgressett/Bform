using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Comments;

public class Comment: IDataModel
{
    [BsonId]
    public Guid Id { get; set; }

    public int Version { get; set; }
    public Guid UserID { get; set; }
        
    public string CommentText { get; set; } = "";

    public bool IsChildComment { get; set; }
    
    public Guid HostEntity { get; set; }
    public Guid? ParentComment { get; set; }
    public string HostType { get; set; } = "";

    public Guid WorkSet { get; set; }
    public Guid WorkItem { get; set; }

    public DateTime PostDate { get; set; }
}
