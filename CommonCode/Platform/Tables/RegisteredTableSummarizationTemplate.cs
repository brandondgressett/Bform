using BFormDomain.CommonCode.Platform.Content;

namespace BFormDomain.CommonCode.Platform.Tables;

public class RegisteredTableSummarizationTemplate: IContentType
{
    #region IContentType
    public string Name { get; set; } = null!;
    public int DescendingOrder { get; set; }
    public string? DomainName { get; set; } = nameof(RegisteredTableSummarizationTemplate);

    public Dictionary<string, string>? SatelliteData { get; } = new();

    public List<string> Tags { get; set; } = new();
    #endregion

    public TableSummarizationCommand Summarization { get; set; } = null!;
}
