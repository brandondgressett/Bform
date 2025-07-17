using Newtonsoft.Json.Linq;


namespace BFormDomain.CommonCode.Platform.Tables;

public class TableSummaryRow
{
    public string Group { get; set; } = null!;
    public double Summary { get; set; }

    public List<JObject>? Details { get; set; } 

    public TableQueryCommand? DetailQuery { get; set; }
}
