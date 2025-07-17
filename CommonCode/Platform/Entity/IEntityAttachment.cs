namespace BFormDomain.CommonCode.Platform.Entity;

public interface IEntityAttachment
{
    string? AttachedEntityType { get; set; }
    Guid? AttachedEntityId { get; set; }
    string? AttachedEntityTemplate { get; set; }
}
