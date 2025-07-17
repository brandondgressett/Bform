using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Notification;

public enum ToastMessages
{
    ToastExchange,
    ToastNotify
}

public class UserToast: IDataModel
{
    [BsonId]
    public Guid Id { get; set; }

    public int Version { get; set; }

    public Guid UserId { get; set; }
    public DateTime Created { get; set; }

    public string Subject { get; set; } = null!;
    public string Details { get; set; } = null!;

}
