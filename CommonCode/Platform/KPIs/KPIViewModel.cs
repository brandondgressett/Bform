using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.Comments;
using BFormDomain.CommonCode.Platform.WorkItems;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Repository;
using BFormDomain.Validation;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.KPIs;


public class KPIViewModel
{
    public Guid Id { get; set; }
    public string? IconClass { get; set; } = null!;
    

    public string Title { get; set; } = null!;
    public List<string> Subject { get; set; } = new();

    public List<KPISamplesViewModel> Samples { get; set; } = new();
    public List<KPISignalsViewModel> Signals { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    

    public static async Task<KPIViewModel> Create(
        KPITemplate template,
        KPIInstance instance,
        IApplicationTerms terms,
        IRepository<KPIData> data,
        UserInformationCache users,
        IRepository<WorkSet> workSets,
        IRepository<WorkItem> workItems,
        DateTime? begin,
        DateTime? end,
        string? tzid)
    {
        if (end is null)
        {
            end = DateTime.UtcNow;
            begin = template.ViewTimeFrame.BackFrom(end.Value);
        }

        var (rawData, _) = await data.GetAllOrderedAsync<DateTime>(
            k => k.SampleTime, false,
            k => k.KPITemplateName == template.Name && k.KPIInstanceId == instance.Id);

        var samplesVMs = new Dictionary<int, KPISamplesViewModel>();
        var signalsVMs = new Dictionary<int, KPISignalsViewModel>();
        var localTz = tzid is null ? TimeZoneInfo.Utc : TimeZoneInfo.FromSerializedString(tzid);

        foreach (var k in rawData)
        {
            ConvertSamples(template, terms, samplesVMs, localTz, k);
            ConvertSignals(template, terms, signalsVMs, localTz, k);
        }

        List<string> subjects = await ComposeSubjects(
            template, instance, terms, users, workSets, workItems);

        var retval = new KPIViewModel
        {
            Id = instance.Id,
            IconClass = template.IconClass,
            Title = terms.ReplaceTerms(template.Title),
            Subject = subjects,
            Tags = instance.Tags,
            Samples = samplesVMs.Values.ToList(),
            Signals = signalsVMs.Values.ToList()
        };

        return retval;

        static void ConvertSamples(KPITemplate template, IApplicationTerms terms, Dictionary<int, KPISamplesViewModel> samplesVMs, TimeZoneInfo localTz, KPIData k)
        {
            foreach (var s in k.Samples)
            {
                if (!samplesVMs.ContainsKey(s.Id))
                {
                    var sTemplate =
                        template
                            .ComputeStages
                            .FirstOrDefault(
                                it => it.ComputeType != KPIComputeType.Intermediate
                                      && it.SampleId == s.Id)!;
                    sTemplate.Guarantees($"content KPI {template.Name} Sample id {s.Id} has no template").IsNotNull();
                    sTemplate.Title.Guarantees($"content KPI {template.Name} sample id {s.Id} is being presented and must have a title.").IsNotNullOrEmpty();

                    samplesVMs.Add(s.Id, new KPISamplesViewModel
                    {
                        IsMain = sTemplate.ComputeType == KPIComputeType.Main,
                        Title = terms.ReplaceTerms(sTemplate.Title!),
                        Data = new List<KPISampleViewModel>()
                    });

                }

                samplesVMs[s.Id].Data.Add(new KPISampleViewModel
                {
                    Time = TimeZoneInfo.ConvertTimeFromUtc(k.SampleTime, localTz),
                    Value = s.Value
                });


            }
        }

        static void ConvertSignals(KPITemplate template, IApplicationTerms terms, Dictionary<int, KPISignalsViewModel> signalsVMs, TimeZoneInfo localTz, KPIData k)
        {
            foreach (var s in k.Signals)
            {
                if (!signalsVMs.ContainsKey(s.SignalId))
                {
                    var sTemplate =
                        template
                            .SignalStages
                            .FirstOrDefault(it => it.SignalId == s.SignalId)!;
                    sTemplate.Guarantees($"content KPI {template.Name} signal id {s.SignalId} has no template").IsNotNull();
                    sTemplate.Title.Guarantees($"content KPI {template.Name} signal id {s.SignalId} is being presented and must have a title.").IsNotNullOrEmpty();

                    signalsVMs.Add(s.SignalId, new KPISignalsViewModel
                    {
                        Title = terms.ReplaceTerms(sTemplate.Title!),
                        Data = new List<KPISignalViewModel>(),
                    });

                }


                signalsVMs[s.SignalId].Data.Add(new KPISignalViewModel
                {
                    SignalTime = TimeZoneInfo.ConvertTimeFromUtc(k.SampleTime, localTz),
                    SignalType = s.SignalType,
                    Value = s.SignalValue
                });

            }
        }

        static async Task<List<string>> ComposeSubjects(KPITemplate template, KPIInstance instance, IApplicationTerms terms, UserInformationCache users, IRepository<WorkSet> workSets, IRepository<WorkItem> workItems)
        {
            var subjects = new List<string>();
            if (instance.SubjectUser is not null)
            {
                var user = (await users.Fetch(instance.SubjectUser.Value))!;
                user.Guarantees().IsNotNull();
                subjects.Add($"ApplicationUser:{user.UserName}");
            }

            var tags = new List<string>();
            foreach (var src in template.Sources)
            {
                if (!string.IsNullOrWhiteSpace(src.TagSubject))
                {
                    tags.Add(src.TagSubject);
                }
            }

            if (tags.Any())
            {
                subjects.Add($"Tags: [{string.Join(',', tags)}]");
            }

            if (instance.SubjectWorkSet is not null)
            {
                var (ws, _) = await workSets.LoadAsync(instance.SubjectWorkSet.Value)!;
                ws.Guarantees().IsNotNull();
                var wsTerm = terms.ReplaceTerms("WorkSet");
                subjects.Add($"{wsTerm}: {ws.Title}");
            }

            if (instance.SubjectWorkItem is not null)
            {
                var (wi, _) = await workItems.LoadAsync(instance.SubjectWorkItem.Value)!;
                wi.Guarantees().IsNotNull();
                var wiTerm = terms.ReplaceTerms("WorkItem");
                subjects.Add($"{wiTerm}: {wi.Title}");
            }

            return subjects;
        }
    }


}
