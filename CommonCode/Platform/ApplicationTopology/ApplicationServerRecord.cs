using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.ApplicationTopology;

public class ApplicationServerRecord: IDataModel
{

    [BsonId]
    public Guid Id { get; set; }

    public int Version { get; set; }

    public string ServerName { get; set; } = "";

    public DateTime LastPingTime { get; set; }

    public List<string> ServerRoles { get; set; } = new List<string>();


    
    
}
