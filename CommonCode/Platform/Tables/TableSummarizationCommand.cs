using BFormDomain.CommonCode.Utility;
using BFormDomain.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;


namespace BFormDomain.CommonCode.Platform.Tables;

/// <summary>
/// TableSummarizationCommand creates a TableSummaryViewModel based on the properties assigned
///     -References:
///         >RegisteredTableSummarizationTemplate.cs
///         >RuleActionSummarizeTable.cs
///         >TableLogic.cs
///     -Functions:
///         >Create
/// </summary>
public class TableSummarizationCommand
{
    public string? Name { get; set; } = null!;
    public string? Title { get; set; }
    public string? Description { get; set; }

    public bool? IsVisibleToUsers { get; set; } = true;
    public bool DisplayMasterDetail { get; set; }
    public string? DetailFormTemplate { get; set; }

    public string? IconClass { get; set; }


    public int TakeSourceRows { get; set; }
    public int TakeSummaryRows { get; set; }

    public string GroupingBy { get; set; } = null!;
    public string SummaryField { get; set; } = null!;

    [JsonConverter(typeof(StringEnumConverter))]
    public SummaryComputation Computation { get; set; }

    public bool IncludeGroupRows { get; set; }
    public bool IncludeGroupTableQueryCommand { get; set; }

    public TableSummaryViewModel Create(
        TableViewModel tvm, 
        TableTemplate template,
        TableQueryCommand? command = null)
    {
        var inputData = tvm.RawData;

        var retval = new TableSummaryViewModel
        {
            Name = Name ?? tvm.Name,
            Title = Title ?? tvm.Title,
            Description = Description ?? tvm.Description,
            IsVisibleToUsers = IsVisibleToUsers ?? tvm.IsVisibleToUsers,
            DisplayMasterDetail = DisplayMasterDetail,
            DetailFormTemplate = DetailFormTemplate,
            IconClass = IconClass ?? tvm.IconClass,
            InnerColumns =  IncludeGroupRows ? tvm.Columns: null,
            InnerAgGridColumnDefsJson = IncludeGroupRows ? tvm.AgGridColumnDefsJson: null
        };

        if(TakeSourceRows > 0)
            inputData = inputData.Take(TakeSourceRows).ToList();

        var withJson = (from it in inputData
                       let jsonText = it.PropertyBag!.ToJsonString()
                       let json = JObject.Parse(jsonText)
                       let gtoken = json[GroupingBy]
                       let gp = gtoken is null ? "none" : gtoken.Value<string>()
                       let vtoken = json[SummaryField]
                       let v = vtoken is null ? 0.0 : vtoken.Value<double>()
                       select new { row = it, json = json, gp = gp, val = v }).ToList();

        var grouped = withJson.GroupBy(it => it.gp);
        if(TakeSummaryRows > 0)
            grouped = grouped.Take(TakeSummaryRows);

        var resultData = new List<TableSummaryRow>();
        foreach(var gp in grouped)
        {
            double summaryValue = 0.0;
            var vals = gp.Select(it => it.val);

            switch(Computation)
            {
                case SummaryComputation.Count:
                    summaryValue = vals.Count();
                    break;
                case SummaryComputation.Sum:
                    summaryValue = vals.Sum();
                    break;
                case SummaryComputation.Mean:
                    summaryValue = vals.Average();
                    break;
                case SummaryComputation.Median:
                    {
                        var ordered = vals.OrderBy(it => it).ToArray();
                        int mid = ordered.Length / 2;
                        summaryValue = ordered[mid];
                    }
                    break;
                case SummaryComputation.Maximum:
                    summaryValue = vals.Max();
                    break;
                case SummaryComputation.Minimum:
                    summaryValue = vals.Min();
                    break;
            }

            var tsr = new TableSummaryRow
            {
                Group = gp.Key,
                Summary = summaryValue,
            };

            if(IncludeGroupRows)
            {
                tsr.Details = gp.Select(r => TableRowConverter.Create(r.row, template)).ToList();
            }

            if(IncludeGroupTableQueryCommand)
            {
                command.Guarantees().IsNotNull();

                // clone object by serializing / deserializing
                var cloneJson = JsonConvert.SerializeObject(command);
                tsr.DetailQuery = JsonConvert.DeserializeObject<TableQueryCommand>(cloneJson)!;
                tsr.DetailQuery.ColumnFilters.Add(new ColumnFilter(Field: GroupingBy, Value: gp.Key));
            }


            resultData.Add(tsr);


        }

        retval.Data = resultData;


        return retval;
    }
    

}
