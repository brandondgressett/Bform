namespace BFormDomain.CommonCode.Platform.Tables;

public class TableSummaryViewModel
{
    public string Name { get; set; } = null!;
    
    public string? Title { get; set; } = null!;
    public string? Description { get; set; }

    public bool IsVisibleToUsers { get; set; } = true;
    public bool DisplayMasterDetail { get; set; }
    public string? DetailFormTemplate { get; set; }

    public string? IconClass { get; set; }

    public List<ColDef>? InnerColumns { get; set; } = new();
    public string? InnerAgGridColumnDefsJson { get; set; }

    public List<TableSummaryRow> Data { get; set; } = new();

}
