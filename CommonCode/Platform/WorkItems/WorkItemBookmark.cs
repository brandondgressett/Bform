namespace BFormDomain.CommonCode.Platform.WorkItems;

public class WorkItemBookmark
{
    public Guid ID { get; set; }
    public DateTime Created { get; set; }
    public string Title { get; set; } = null!;
    public Guid ApplicationUser { get; set; }
}
