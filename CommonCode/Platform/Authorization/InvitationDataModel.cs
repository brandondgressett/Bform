using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.Authorization;

public class InvitationDataModel : IDataModel
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }


    public string EmailTarget { get; set; } = "";
    public DateTime Expiration { get; set; }
    public string InvitationCode { get; set; } = "";
    public List<string> InvitedRoles { get; set; } = new List<string>();


}
