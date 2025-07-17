using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Authorization;

public class RefreshToken : IDataModel {
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }

    public Guid UserId { get; set; }
    public string Token { get; set; } = "";
    public string JwtId { get; set; } = "";
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime Added { get; set; }
    public DateTime ExpiryDate { get; set; }
}
