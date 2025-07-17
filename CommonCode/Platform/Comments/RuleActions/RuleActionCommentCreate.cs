using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Comments.RuleActions;

/// <summary>
/// RuleActionCommentCreate creates new comments using CommentLogic through the rules engine service
///     References:
///         >Service
///     -Functions:
///         >Execute
/// </summary>
public class RuleActionCommentCreate : IRuleActionEvaluator
{
    private readonly CommentsLogic _logic;
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;

    public string Name => RuleUtil.FixActionName(nameof(RuleActionCommentCreate));

    public RuleActionCommentCreate(
        CommentsLogic logic, 
        IApplicationAlert alerts,
        IApplicationTerms terms)
    {
        _logic = logic;
        _alerts = alerts;
        _terms = terms;
    }

    public class Arguments
    {
        public string? Text { get; set; }
        public string? TextQuery { get; set; }
        public string? UserIdQuery { get; set; }

        public string? ParentQuery { get; set; }

        public string? HostWorkSetIdQuery { get; set; }
        public string? HostWorkItemIdQuery { get; set; }
        public string? HostEntityIdQuery { get; set; }
        public string? HostEntityTypeQuery { get; set; }
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
        using (PerfTrack.Stopwatch(nameof(RuleActionCommentCreate)))
        {

            try
            {
                args.Requires().IsNotNull();

                string resultProperty = "CreatedComment";
                if (!string.IsNullOrEmpty(result))
                    resultProperty = result;

                var inputs = args!.ToObject<Arguments>();
                inputs.Guarantees().IsNotNull();

                Guid userId = RuleUtil.MaybeLoadProp(eventData, inputs!.UserIdQuery, BuiltIn.SystemUser);
                Guid hostWorkSet = RuleUtil.MaybeLoadProp(eventData, inputs!.HostWorkSetIdQuery, sourceEvent.HostWorkSet!.Value);
                Guid hostWorkItem = RuleUtil.MaybeLoadProp(eventData, inputs!.HostWorkItemIdQuery, sourceEvent.HostWorkItem!.Value);
                Guid hostEntityId = RuleUtil.MaybeLoadProp(eventData, inputs!.HostEntityIdQuery, sourceEvent.OriginEntityId!.Value);
                string? hostEntityType = RuleUtil.MaybeLoadProp(eventData, inputs!.HostEntityTypeQuery, sourceEvent.OriginEntityType);
                Guid? parentComment = RuleUtil.MaybeLoadProp(eventData, inputs!.ParentQuery, Guid.Empty);
                if (parentComment == Guid.Empty)
                    parentComment = null!;

                var commentText = RuleUtil.MaybeLoadProp(eventData, inputs!.TextQuery, inputs!.Text);
                commentText.Guarantees().IsNotNullOrEmpty();
                commentText = _terms.ReplaceTerms(commentText!);

                var origin = sourceEvent.ToPreceding(Name);

                var resultId = await _logic.EventCreateComment(
                    origin,
                    userId,
                    hostWorkSet,
                    hostWorkItem,
                    hostEntityId,
                    hostEntityType!,
                    parentComment,
                    commentText,
                    trx,
                    sealEvents,
                    eventTags);

                var appendix = RuleUtil.GetAppendix(eventData);
                appendix.Add(resultProperty, resultId);
                
            } catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }

        }
        
    }

}
