using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using LinqKit;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tables;

public record class ColumnFilter(string Field, string Value);

/// <summary>
/// TableQueryCommand describes a template for a table query and serves as a translation from command information into a query
///     -References:
///         >RegisteredTableQueryTemplate.cs
///         >RelativeTableQueryCommand.cs
///         >TableLogic.cs
///         >TableSummarizationCommand.cs
///         >TableSummaryRow.cs
///     -Functions:
///         >MakePredicate
///         >ApplyColumnFilters
/// </summary>
public class TableQueryCommand
{
    public string? IdFilter { get; set; }
    public DateTime? EqDateFilter { get; set; }
    public DateTime? LtDateFilter { get; set; }
    public DateTime? GtDateFilter { get; set; }

    public TimeLineQuery? DateTimeLine { get; set; }

    public Guid? UserFilter { get; set; }
    public Guid? WorkSetFilter { get; set; }
    public Guid? WorkItemFilter { get; set; }

    public List<string> MatchAnyTags { get; set; } = new();
    public List<string> MatchAllTags { get; set; } = new();

    public double? LtNumericFilter { get; set; }
    public double? GtNumericFilter { get; set; }
    public double? EqNumericFilter { get; set; }

    [JsonConverter(typeof(OrderingTypeConverter))]
    public QueryOrdering Ordering { get; set; }

    public List<ColumnFilter> ColumnFilters { get; set; } = new();

    public ExpressionStarter<TableRowData> MakePredicate()
    {
        var predicate = PredicateBuilder.New<TableRowData>();

        bool hasFilter = IdFilter is not null ||
                         EqDateFilter is not null ||
                         LtDateFilter is not null ||
                         GtDateFilter is not null ||
                         UserFilter is not null ||
                         WorkSetFilter is not null ||
                         WorkItemFilter is not null ||
                         MatchAllTags.Any() ||
                         MatchAllTags.Any() ||
                         LtNumericFilter is not null ||
                         GtNumericFilter is not null ||
                         EqNumericFilter is not null ||
                         ColumnFilters.Any();

        if (!hasFilter)
        {
            predicate = predicate.And(tr => true);
            return predicate;
        }

        if (IdFilter is not null)
        {
            predicate = predicate.And(tr => tr.KeyRowId == IdFilter);
        }

        if (DateTimeLine is not null)
        {
            var (start, end) = DateTimeLine.Resolve();
            predicate = predicate.And(tr=>tr.KeyDate >= start && tr.KeyDate <= end);   
        }
        else
        {

            if (EqDateFilter is not null)
            {
                predicate = predicate.And(tr => tr.KeyDate == EqDateFilter.Value);
            }
            else
            {
                if (LtDateFilter is not null)
                {
                    predicate = predicate.And(tr => tr.KeyDate <= LtDateFilter.Value);
                }

                if (GtDateFilter is not null)
                {
                    predicate = predicate.And(tr => tr.KeyDate >= GtDateFilter.Value);
                }
            }
        }

        

        if (UserFilter is not null)
            predicate = predicate.And(tr => tr.KeyUser == UserFilter.Value);

        if (WorkSetFilter is not null)
            predicate = predicate.And(tr => tr.KeyWorkSet == WorkSetFilter.Value);

        if (WorkItemFilter is not null)
            predicate = predicate.And(tr => tr.KeyWorkItem == WorkItemFilter.Value);

        if (MatchAllTags.Any())
            predicate = predicate.And(tr => MatchAllTags.All(tg => tr.Tags.Contains(tg)));

        if (MatchAnyTags.Any())
            predicate = predicate.And(tr => MatchAnyTags.Any(tg => tr.Tags.Contains(tg)));

        if (EqNumericFilter is not null)
        {
            predicate = predicate.And(tr => tr.KeyNumeric == EqNumericFilter.Value);
        }
        else
        {
            if (LtNumericFilter is not null)
                predicate = predicate.And(tr => tr.KeyNumeric < LtNumericFilter);

            if (GtNumericFilter is not null)
                predicate = predicate.And(tr => tr.KeyNumeric > GtNumericFilter);
        }

        

        return predicate!;
    }

    public IEnumerable<TableRowData> ApplyColumnFilters(IEnumerable<TableRowData> inputData)
    {
        var retval = inputData;
        if(ColumnFilters.Any())
        {
            var predicate = PredicateBuilder.New<TableRowData>();
            foreach(var cf in ColumnFilters)
            {
                predicate = predicate.And(tr => tr.PropertyBag![cf.Field].ToString() == cf.Value);
            }

            retval = inputData.Where(predicate);
        }

        return retval;
    }



}
