using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace BFormDomain.CommonCode.Platform.Forms.RuleActions;

public class RuleActionLookupForm: IRuleActionEvaluator
{
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly IRepository<FormInstance> _instances;

    public RuleActionLookupForm(
        IRepository<FormInstance> instances,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _instances = instances;
        _alerts = alerts;
        _terms = terms;
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionLookupForm));

    public enum Selection
    {
        First,
        Array,
        Recent,
        Oldest,
    }

    public class Arguments
    {
        
        // id, template, tags, workset, workitem
        public string? IdQuery { get; set; }
        public string? Template { get; set; }
        public string? TemplateQuery { get; set; }
        public string? WorkSetQuery { get; set; }
        public string? WorkItemQuery { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Selection Selection { get; set; }
        
        

    }

    public async Task Execute(
        ITransactionContext trx,
        string? result,
        JObject eventData,
        JObject? args,
        AppEvent sourceEvent,
        bool sealEvents,
        IEnumerable<string>? eventTags = null)
    {
        using (PerfTrack.Stopwatch(nameof(RuleActionEditFormProperty)))
        {

            try
            {
                args.Requires().IsNotNull();

                string resultProperty = "SelectedForm";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();
                
                var template = RuleUtil.MaybeLoadProp(eventData, inputs.TemplateQuery, inputs.Template);
                var id = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.IdQuery, null);
                var workSet = RuleUtil.MaybeLoadProp(eventData, inputs.WorkSetQuery, sourceEvent.HostWorkSet);
                var workItem = RuleUtil.MaybeLoadProp(eventData, inputs.WorkItemQuery, sourceEvent.HostWorkItem);

                Expression<Func<FormInstance, bool>> predicate =
                    fi => fi.HostWorkSet == workSet && fi.HostWorkItem == workItem;

                if (!string.IsNullOrWhiteSpace(template)) 
                {
                    var compiled = predicate.Compile();
                    predicate = fi => compiled(fi) && fi.Template == template;
                }

                if(id is not null)
                {
                    var compiled = predicate.Compile();
                    predicate = fi=>compiled(fi) && fi.Id == id;    
                }

                var appendix = RuleUtil.GetAppendix(eventData);

                var (matches, _) = await _instances.GetAllAsync(predicate);
                
                if(matches.Any())
                {
                    matches = matches.OrderByDescending(fi => fi.UpdatedDate).ToList();

                    if (inputs.Selection == Selection.Array)
                    {
                        appendix.Add(resultProperty, new JArray(matches.Select(fi => JObject.FromObject(fi) )));
                    }
                    else
                    {
                        FormInstance? instance = null!;
                        instance = inputs.Selection switch
                        {
                            Selection.First or Selection.Recent => matches.First(),
                            _ => matches.Last(),
                        };

                        appendix.Add(resultProperty, JObject.FromObject(instance));
                    }

                }
                


            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }


}
