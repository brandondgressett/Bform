using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Notification;

public class NotificationAudit : IDataModel {
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }

    public List<NotificationAuditEvent> Items { get; set; } = new List<NotificationAuditEvent>();

    public Guid UserRef { get; set; } = Guid.Empty;

    public DateTime Created { get; set; }


    public NotificationAudit()
    {

    }

    public NotificationAudit(Guid userRef, params NotificationAuditEvent[] events)
    {
        Id = Guid.NewGuid();
        UserRef = userRef;
        Created = DateTime.UtcNow;
        Items.AddRange(events);
    }

    

}
