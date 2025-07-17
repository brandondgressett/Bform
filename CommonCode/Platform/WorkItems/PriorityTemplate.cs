namespace BFormDomain.CommonCode.Platform.WorkItems;

public class PriorityTemplate
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public bool IsDefault { get; set; }

}
