using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Notification;

public class NotificationContact : IDataModel
{
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int Version { get; set; } = 0;

    public Guid UserRef { get; set; } = Guid.Empty;

    public NotificationTimeSeverityTable TimeSeverityTable { get; set; } = new NotificationTimeSeverityTable();

    
    public string? EmailAddress { get; set; } = null!;
    public string? TextNumber { get; set; } = null!;
    public string? CallNumber { get; set; } = null!;

    public string ContactTitle { get; set; } = "";

    public string ContactNotes { get; set; } = "";

    // use TimeZoneInfo.ToSerializedString() / TimeZoneInfo.FromSerializedString
    public string TimeZoneInfoId { get; set; } = "";

    public bool Active { get; set; } = true;
}
