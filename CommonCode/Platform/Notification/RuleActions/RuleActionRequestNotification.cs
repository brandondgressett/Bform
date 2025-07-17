using BFormDomain.CommonCode.Platform;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Rules;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Notification.RuleActions;

public class RuleActionRequestNotification : IRuleActionEvaluator
{
    private readonly RequestNotification _req;
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly IRepository<NotificationGroup> _groups;
    private readonly IRepository<NotificationContact> _contacts;

    public RuleActionRequestNotification(
        RequestNotification rq,
        IApplicationTerms terms,
        IRepository<NotificationGroup> groups,
        IRepository<NotificationContact> contacts,
        IApplicationAlert alerts)
    {
        _req = rq;
        _terms = terms;
        _groups = groups;
        _contacts = contacts;
        _alerts = alerts;   
    }

    public string Name => RuleUtil.FixActionName(nameof(RuleActionRequestNotification));

    public class Arguments
    {
        public bool? Contact { get; set; }
        public string? ContactProp { get; set; }

        public string? GroupProp { get; set; }
        public string? GroupByTitle { get; set; }
        
        public List<string>? GroupByTags { get; set; }

        public bool? MultipleGroups { get; set; }

        public string? Subject { get; set; }
        public string? SubjectProp { get; set; }

        public string? UserProp { get; set; }

        public string? SMSText { get; set; }
        public string? SMSTextProp { get; set; }

        public string? EmailText { get; set; }
        public string? EmailTextProp { get; set; }
        public string? ToastText { get; set; }
        public string? ToastTextProp { get; set; }

        public string? CallText { get; set; }
        public string? CallTextProp { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel Severity { get; set; }

        public bool WantDigest { get; set; } = false;
        public bool WantSuppression { get; set; } = false;

        public int SuppressionMinutes { get; set; } = 240;
        public int DigestMinutes { get; set; } = 480;

        public int DigestHead { get; set; } = 5;
        public int DigestTail { get; set; } = 5;

    }

    public async Task Execute(ITransactionContext trx, string? result, JObject eventData, JObject? args, AppEvent sourceEvent, bool sealEvents, IEnumerable<string>? eventTags = null)
    {
        using(PerfTrack.Stopwatch(nameof(RuleActionRequestNotification)))
        {
            try
            {
                args.Requires().IsNotNull();

                var inputs = args!.ToObject<Arguments>()!;
                inputs.Guarantees().IsNotNull();

                string? subject = RuleUtil.MaybeLoadProp(eventData, inputs.SubjectProp, inputs!.Subject);
                subject.Guarantees().IsNotNullOrEmpty();

                var defaultUser = sourceEvent.OriginUser ?? BuiltIn.SystemUser;
                Guid userId = RuleUtil.MaybeLoadProp(eventData, inputs.UserProp, defaultUser);

                Guid? contact = null!;
                Guid? group = null!;
                List<Guid> groups = new();


                if(inputs.Contact is not null)
                {
                    var (nc,_) = await _contacts.GetOneAsync(nc => nc.Active && nc.UserRef == userId);
                    nc.Guarantees().IsNotNull();
                    contact = nc!.Id;
                } 
                
                if(inputs.ContactProp is not null)
                {
                    var candidate = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.ContactProp, null);
                    candidate.Guarantees().IsNotNull();
                    contact = candidate!.Value;
                } 
                
                if(inputs.GroupProp is not null)
                {
                    var candidate = RuleUtil.MaybeLoadProp<Guid?>(eventData, inputs.GroupProp, null);
                    candidate.Guarantees().IsNotNull();
                    group = candidate!.Value;
                } 
                
                if(inputs.GroupByTitle is not null)
                {
                    var (ng, _) = await _groups.GetOneAsync(ng => ng.Active && ng.GroupTitle.Contains(inputs.GroupByTitle));
                    ng.Guarantees().IsNotNull();
                    group = ng!.Id;
                } 
                
                if(inputs.GroupByTags is not null && inputs.GroupByTags.Any())
                {
                    var (ngs, _) = await _groups.GetAllAsync(ng => ng.Active && inputs.GroupByTags.Any(tg => ng.Tags.Contains(tg)));
                    if (ngs is not null && ngs.Any())
                    {
                        
                        if (inputs.MultipleGroups ?? false)
                        {
                            groups.AddRange(ngs.Select(ng => ng.Id));    
                        }
                        else
                        {
                            ngs = ngs.OrderByDescending(ng => ng.OverlapScore(inputs.GroupByTags)).ToList();
                            group = ngs.First().Id;
                        }
                    }
                }

                bool haveTarget = contact.HasValue || group.HasValue || groups.Any();
                haveTarget.Guarantees().IsTrue();

                
                var smsText = RuleUtil.MaybeLoadProp(eventData, inputs.SMSTextProp, inputs.SMSText);
                if(smsText is not null) smsText = _terms.ReplaceTerms(smsText);

                var emailText = RuleUtil.MaybeLoadProp(eventData, inputs.EmailTextProp, inputs.EmailText);
                if(emailText is not null) emailText = _terms.ReplaceTerms(emailText);

                var toastText = RuleUtil.MaybeLoadProp(eventData, inputs.ToastTextProp, inputs.ToastText);
                if(toastText is not null) toastText = _terms.ReplaceTerms(toastText);

                var callText = RuleUtil.MaybeLoadProp(eventData, inputs.CallTextProp, inputs.CallText);
                if(callText is not null) callText = _terms.ReplaceTerms(callText);  

                var request = new NotificationMessage
                {
                    Subject = subject!,
                    CreatorId = userId.ToString(),
                    SMSText = smsText,
                    EmailHtmlText = emailText,
                    ToastText = toastText,
                    CallText = callText,
                    NotificationGroups = groups,
                    NotificationGroup = group,
                    NotificationContact = contact,
                    Severity = inputs.Severity,
                    WantDigest = inputs.WantDigest,
                    WantSuppression = inputs.WantSuppression,
                    SuppressionMinutes = inputs.SuppressionMinutes,
                    DigestMinutes = inputs.DigestMinutes,
                    DigestHead = inputs.DigestHead,
                    DigestTail = inputs.DigestTail
                };

                await _req.Request(request);


            } catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }
        }
    }
}
