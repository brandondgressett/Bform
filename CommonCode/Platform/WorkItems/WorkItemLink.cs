namespace BFormDomain.CommonCode.Platform.WorkItems;

public class WorkItemLink
{
    public Guid Id { get; set; }
    public Guid WorkSetId { get; set; }
    public Guid WorkItemId { get; set; }
    public string Title { get; set; } = null!;
    public DateTime LinkCreated { get; set; }
}
