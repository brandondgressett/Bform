namespace BFormDomain.CommonCode.Platform.KPIs;

public class CreateKPIInstanceCommand
{
    public string? TemplateName { get; set; } = null!;

    public List<string>? InitialTags { get; set; }

    public List<string>? WorkSetHostTags { get; set; } = null!;
    public List<string>? WorkItemHostTags { get; set; } = null!;

    public List<string>? WorkSetSubjectTags { get; set; } = null!;
    public List<string>? WorkItemSubjectTags { get; set; } = null!;

}
