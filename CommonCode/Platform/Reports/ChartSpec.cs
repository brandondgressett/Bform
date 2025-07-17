namespace BFormDomain.CommonCode.Platform.Reports;

public class ChartSpec
{
    public bool IncludeChart { get; set; }
    public string ChartTitle { get; set; } = null!;
    public bool ChartShowAtBottom { get; set; }
    public string? ChartChangeOnField { get; set; }
    public string ChartValueField { get; set; } = "Count";
    public bool ChartShowBorder { get; set; } 
    public string ChartLabelHeader { get; set; }  = "Label";
    public string ChartPercentageHeader { get; set; } = "Percentage";
    public string ChartValueHeader { get; set; } = "Value";
}
