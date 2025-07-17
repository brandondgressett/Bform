using BFormDomain.DataModels;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public class WorkSetMember : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }

    public Guid WorkSetId { get; set; }
    public Guid UserId { get; set; }
}
