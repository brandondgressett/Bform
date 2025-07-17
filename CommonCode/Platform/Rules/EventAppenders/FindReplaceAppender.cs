using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules.EventAppenders;
/// <summary>
/// 
///     -References:
///         >RuleEvaluator.cs
///         >RuleServiceCollectionExtensions.cs
///     -Functions:
///         >AddToAppendix
/// </summary>
public class FindReplaceAppender : IEventAppender
{

    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;

    public FindReplaceAppender(
        IApplicationAlert alerts,
        IApplicationTerms terms) 
    {
        _alerts = alerts;
        _terms = terms;
    }

    public string Name => EventAppenderUtility.FixName(nameof(FindReplaceAppender));

    public class Replacement
    {
        public string Find { get; set; } = null!;
        public string? Replace { get; set; } 
        public string? ReplaceQuery { get; set; } 
    }

    public class Arguments
    {
        public string? Source { get; set; }
        public string? SourceQuery { get; set; }
        public bool ReplaceTerms { get; set; } = true;

        public List<Replacement> Do { get; set; } = new();
    }


    public Task AddToAppendix(
         string? resultName,
         JObject eventData,
         JObject? appendArguments)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resultName))
                resultName = Name;

            appendArguments.Requires().IsNotNull();
            var args = appendArguments!.ToObject<Arguments>()!;
            args.Requires().IsNotNull();

            string source = RuleUtil.MaybeLoadProp(eventData, args.SourceQuery, args.Source)!;
            source.Requires().IsNotNullOrEmpty();

            args.Do.Requires().IsNotEmpty();

            foreach(var rep in args.Do)
            {
                var replacement = RuleUtil.MaybeLoadProp(eventData, rep.ReplaceQuery, rep.Replace);
                if(replacement is not null)
                {
                    source = source.Replace(rep.Find, replacement);
                }

            }

            source = _terms.ReplaceTerms(source);

            var appendix = RuleUtil.GetAppendix(eventData);
            appendix.Add(resultName, source);

        } catch(Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General,
                Microsoft.Extensions.Logging.LogLevel.Information,
                ex.TraceInformation(),
                2);
        }


        return Task.CompletedTask;
    }
}
