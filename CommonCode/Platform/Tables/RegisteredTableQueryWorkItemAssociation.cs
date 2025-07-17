using BFormDomain.DataModels;

namespace BFormDomain.CommonCode.Platform.Tables;

public class RegisteredTableQueryWorkItemAssociation : IDataModel
{
    public Guid Id { get; set; }
    public int Version { get; set; }

    public string TableTemplateName { get; set; } = null!;
    public string RegisteredQueryTemplateName { get; set; } = null!;
    public string? RegisteredSummaryTemplateName { get; set; }

    public string Uri { get; set; } = null!;
    public int? Page { get; set; }

    public Guid WorkItem { get; set; }
}
