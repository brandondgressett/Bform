using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Comments.RuleActions;


/// <summary>
/// RuleActionCommentEdit edits comments using CommentLogic through the rules engine service
///     References:
///         >Service
///     -Functions:
///         >Execute
/// </summary>
public class RuleActionCommentEdit : IRuleActionEvaluator
{
    private readonly CommentsLogic _logic;
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;

    public string Name => RuleUtil.FixActionName(nameof(RuleActionCommentEdit));

    public RuleActionCommentEdit(CommentsLogic logic,
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _logic = logic;
        _alerts = alerts;
        _terms = terms;
    }


    public class Arguments
    {
        [JsonProperty(Required = Required.Always)]
        public string CommentIdQuery { get; set; } = "";
        public string? Text { get; set; }
        public string? TextQuery { get; set; }
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
        using(PerfTrack.Stopwatch(nameof(RuleActionCommentEdit)))
        {
            try
            {
                args.Requires().IsNotNull();

                var inputs = args!.ToObject<Arguments>();
                inputs.Guarantees().IsNotNull();

                Guid commentId = RuleUtil.MaybeLoadProp(eventData, inputs!.CommentIdQuery, Guid.Empty);
                commentId.Guarantees().IsNotEqualTo(Guid.Empty);

                var commentText = RuleUtil.MaybeLoadProp(eventData, inputs!.TextQuery, inputs!.Text);
                commentText.Guarantees().IsNotNullOrEmpty();
                commentText = _terms.ReplaceTerms(commentText!);

                var origin = sourceEvent.ToPreceding(Name);

                await _logic.EventEditComment(
                    commentId,
                    commentText,
                    origin,
                    trx,
                    sealEvents,
                    eventTags);


            } catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
        
    }
}
