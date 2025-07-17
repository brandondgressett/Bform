using BFormDomain.CommonCode.Platform.Rules;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tables;


/// <summary>
/// RelativeTableQueryCommand builds a table query using BuildQuery via TableTemplate
///     -References:
///         >ReportTemplate.cs
///         >RukeActionSelectTableRow.cs
///         >RukeActionSummarizeTable.cs
///     -Functions:
///         >BuildQuery
/// </summary>
public class RelativeTableQueryCommand
{
    [JsonRequired]
    public string TableTemplate { get; set; } = null!;

    public string? IdFilter { get; set; }
    public string? IdFilterQuery { get; set; }

    public DateTime? EqDateFilter { get; set; }
    public string? EqDateFilterQuery { get; set; }

    public DateTime? LtDateFilter { get; set; }
    public string? LtDateFilterQuery { get; set; }

    public DateTime? GtDateFilter { get; set; }
    public string? GtDateFilterQuery { get; set; }

    public int SecondsBack { get; set; }
    public int MinutesBack { get; set; }
    public int HoursBack { get; set; }
    public int DaysBack { get; set; }
    public int MonthsBack { get; set; }

    public Guid? UserFilter { get; set; }
    public string? UserFilterQuery { get; set; }

    public Guid? WorkSetFilter { get; set; }
    public string? WorkSetFilterQuery { get; set; }

    public Guid? WorkItemFilter { get; set; }
    public string? WorkItemFilterQuery { get; set; }

    public List<string> MatchAnyTags { get; set; } = new();
    public string? MatchAnyTagsQuery { get; set; }  
        
    public List<string> MatchAllTags { get; set; } = new();
    public string? MatchAllTagsQuery { get; set; }


    public double? LtNumericFilter { get; set; }
    public string? LtNumericFilterQuery { get; set; }

    public double? GtNumericFilter { get; set; }
    public string? GtNumericFilterQuery { get; set; }

    public double? EqNumericFilter { get; set; }
    public string? EqNumericFilterQuery { get; set; }

    public Guid? WorkSetSpecificData { get; set; }
    public string? WorkSetSpecificDataQuery { get; set; }
    public Guid? WorkItemSpecificData { get; set; }
    public string? WorkItemSpecificDataQuery { get; set; }


    [JsonConverter(typeof(StringEnumConverter))]
    public QueryOrdering Ordering { get; set; }

    public List<ColumnFilter> ColumnFilters { get; set; } = new();

    public TableQueryCommand BuildQuery(JObject data, out Guid? wsSpec, out Guid? wiSpec)
    {

        string? idFilter = RuleUtil.MaybeLoadProp<string?>(data, IdFilterQuery, IdFilter);
        DateTime? eqDateFilter = RuleUtil.MaybeLoadProp<DateTime?>(data, EqDateFilterQuery, EqDateFilter);
        DateTime? ltDateFilter = RuleUtil.MaybeLoadProp<DateTime?>(data, LtDateFilterQuery, LtDateFilter);
        DateTime? gtDateFilter = RuleUtil.MaybeLoadProp<DateTime?>(data, GtDateFilterQuery, GtDateFilter);

        var sum = SecondsBack + MinutesBack + HoursBack + DaysBack + MonthsBack;
        if(sum > 0)
        {
            eqDateFilter = null!;
            ltDateFilter = null!;

            var startTime = DateTime.UtcNow.AddSeconds(-SecondsBack)
                                           .AddMinutes(-MinutesBack)
                                           .AddHours(-HoursBack)
                                           .AddDays(-DaysBack)
                                           .AddMonths(-MonthsBack);

            gtDateFilter = startTime;

        }


        Guid? userFilter = RuleUtil.MaybeLoadProp<Guid?>(data, UserFilterQuery, UserFilter);
        Guid? wsFilter = RuleUtil.MaybeLoadProp<Guid?>(data, WorkSetFilterQuery, WorkSetFilter);
        Guid? wiFilter = RuleUtil.MaybeLoadProp<Guid?>(data, WorkItemSpecificDataQuery, WorkItemFilter);


        var matchAnyTags = MatchAnyTags;
        if (!matchAnyTags.Any() && !string.IsNullOrWhiteSpace(MatchAnyTagsQuery))
        {
            var tags = data.SelectToken(MatchAnyTagsQuery)?.Value<string>();
            if (!string.IsNullOrWhiteSpace(tags))
                matchAnyTags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        var matchAllTags = MatchAllTags;
        if (!matchAllTags.Any() && !string.IsNullOrWhiteSpace(MatchAllTagsQuery))
        {
            var tags = data.SelectToken(MatchAllTagsQuery)?.Value<string>();
            if (!string.IsNullOrWhiteSpace(tags))
                matchAllTags = tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        double? ltNumericFilter = RuleUtil.MaybeLoadProp<double?>(data, LtNumericFilterQuery, LtNumericFilter);
        double? gtNumericFilter = RuleUtil.MaybeLoadProp<double?>(data, GtNumericFilterQuery, GtNumericFilter);
        double? eqNumericFilter = RuleUtil.MaybeLoadProp<double?>(data, EqNumericFilterQuery, EqNumericFilter);
        wsSpec = RuleUtil.MaybeLoadProp<Guid?>(data, WorkSetSpecificDataQuery, WorkSetSpecificData);
        wiSpec = RuleUtil.MaybeLoadProp<Guid?>(data, WorkItemSpecificDataQuery, WorkItemSpecificData);

        var query = new TableQueryCommand
        {
            IdFilter = idFilter,
            EqDateFilter = eqDateFilter,
            LtDateFilter = ltDateFilter,
            GtDateFilter = gtDateFilter,
            UserFilter = userFilter,
            MatchAnyTags = matchAnyTags,
            MatchAllTags = matchAllTags,
            LtNumericFilter = ltNumericFilter,
            GtNumericFilter = gtNumericFilter,
            EqNumericFilter = eqNumericFilter,
            WorkSetFilter = wsFilter,
            WorkItemFilter = wiFilter,
            Ordering = Ordering,
            ColumnFilters = ColumnFilters
        };

        return query;
    }
}

