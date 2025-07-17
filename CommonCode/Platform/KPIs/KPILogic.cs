using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Comments;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Platform.WorkItems;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.KPIs;

/// <summary>
/// KPILogic manages CRUD operations for KPI instances
///     -References:
///         >AcceptKPIInstanceContent.cs
///         >KPIDataGroomingService.cs
///         >KPIEntityLoaderModule.cs
///         >KPI rule Action folder
///     -Funtions:
///         >HelpCreateKPIInstance
///         >ActionCreateKPI
///         >EventCreateKPI
///         >EvaluateKPI
///         >EvaluateMatchingKPIs
///         >HelpDeleteKPIInstance
///         >EventDeleteKPIInstance
///         >EventDeleteWorkItemKPIInstances
///         >ActionDeleteKPIInstance
///         >GroomKPIData
///         >GetCreatableKPIs
///         >GetCreatableKPIs
///         >GetRawKPITemplate
///         >GetKPITemplateVM
///         >GetKPIInstances
///         >GetTaggedKPIInstanceRefs
/// </summary>
public class KPILogic
{
    #region fields
    private readonly IApplicationAlert _alerts;
    private readonly IApplicationTerms _terms;
    private readonly IRepository<Comment> _comments;
    private readonly IApplicationPlatformContent _content;
    private readonly ILogger<KPILogic> _logger;
    private readonly AppEventSink _sink;
    private readonly TemplateNamesCache _tnCache;
    private readonly IRepository<KPIInstance> _instances;
    private readonly IRepository<KPIData> _kpiData;
    private readonly KPIEvaluator _kpiEvaluator;
    private readonly IDataEnvironment _dataEnvironment;
    private readonly Tagger _tagger;
    private readonly UserInformationCache _userCache;
    private readonly WorkSetAndItemFinder _workSetAndItemFinder;
    private readonly IRepository<WorkSet> _workSets;
    private readonly IRepository<WorkItem> _workItems;
    #endregion


    public KPILogic(
        IApplicationAlert alerts,
        IApplicationTerms terms,
        IRepository<Comment> comments,
        IApplicationPlatformContent content,
        ILogger<KPILogic> logger,
        AppEventSink sink,
        TemplateNamesCache tnCache,
        IRepository<KPIInstance> instances,
        IRepository<KPIData> kpiData,
        KPIEvaluator kpiEvaluator,
        IDataEnvironment dataEnvironment,
        Tagger tagger,
        UserInformationCache userCache,
        WorkSetAndItemFinder finder,
        IRepository<WorkSet> workSets,
        IRepository<WorkItem> workItems)
    {
        _alerts = alerts;
        _terms = terms;
        _comments = comments;
        _content = content;
        _logger = logger;
        _sink = sink;
        _tnCache = tnCache;
        _instances = instances;
        _kpiData = kpiData;
        _kpiEvaluator = kpiEvaluator;
        _dataEnvironment = dataEnvironment;
        _tagger = tagger;
        _userCache = userCache;
    
        _workSetAndItemFinder = finder;
        _workSets = workSets;
        _workItems = workItems;
    }

    #region creation
    private async Task<(Guid, KPITemplate?)> HelpCreateKPIInstance(
        AppEventOrigin? origin,
        string? action,
        string templateName,
        Guid workSet,
        Guid workItem,
        Guid? userId,
        Guid? userSubject,
        Guid? workSetSubject,
        Guid? workItemSubject,
        bool useWorkSetSubject,
        bool useWorkItemSubject,
        IEnumerable<string>? initialTags,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {
        var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(workSet, workItem);

        var id = Guid.NewGuid();

        // get the template
        var template = _content.GetContentByName<KPITemplate>(templateName)!;
        template.Guarantees().IsNotNull();

        //var scheduleTemplate = _content.GetContentByName<ScheduledEventTemplate>(template.ScheduleTemplate)!;
        //scheduleTemplate.Guarantees().IsNotNull();

        var readyTags = TagUtil.MakeTags(initialTags.EmptyIfNull());

        if (useWorkSetSubject && workSetSubject is null)
            workSetSubject = workSet;
        if(useWorkItemSubject && workItemSubject is null)
            workItemSubject = workItem;


        var kpiInstance = new KPIInstance
        {
            Id=id,
            Template = template.Name,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Creator = Constants.BuiltIn.SystemUser,
            LastModifier = Constants.BuiltIn.SystemUser,
            HostWorkSet = workSet,
            HostWorkItem = workItem,
            SubjectUser = userSubject,
            SubjectWorkSet = useWorkSetSubject ? workSetSubject : null,
            SubjectWorkItem = useWorkItemSubject ? workItemSubject : null,
            Tags = readyTags.ToList(),
            EventTopic = "KPIInstance:" + template.Name
        };

        await _instances.CreateAsync(trx!, kpiInstance);

        foreach (var tag in readyTags)
            await _tagger.CountTags(kpiInstance, trx, 1, tag);

        _sink.BeginBatch(trx!);
        var topic = $"{workSetTemplateName}.{workItemTemplateName}.{templateName}.{context}.kpi_create_instance";
        await _sink.Enqueue(origin, topic, action, kpiInstance, userId, eventTags, seal);
        await _sink.CommitBatch();

        return (id, template);

    }

    public async Task<Guid> ActionCreateKPI(
        string action,
        string templateName,
        Guid workSet,
        Guid workItem,
        Guid userId,
        Guid? userSubject,
        Guid? workSetSubject,
        Guid? workItemSubject,
        bool useWorkSetSubject,
        bool useWorkItemSubject,
        IEnumerable<string>? initialTags,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using(PerfTrack.Stopwatch(nameof(ActionCreateKPI)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (id, template) = await HelpCreateKPIInstance(
                    null, action, templateName,
                    workSet, workItem,
                    userId,
                    userSubject,
                    workSetSubject,
                    workItemSubject,
                    useWorkSetSubject,
                    useWorkItemSubject,
                    initialTags, trx!,
                    seal, "action",
                    null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                return id;

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task<Guid> EventCreateKPI(
        AppEventOrigin origin,
        string templateName,
        Guid? workSet,
        Guid? workItem,
        IEnumerable<string>? workSetHostTags,
        IEnumerable<string>? workItemHostTags,
        Guid userId,
        Guid? userSubject,
        Guid? workSetSubject,
        Guid? workItemSubject,
        IEnumerable<string>? workSetSubjectTags,
        IEnumerable<string>? workItemSubjectTags,
        IEnumerable<string>? initialTags,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventCreateKPI)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {

                bool useWorkSetSubject = workSetSubjectTags is not null || workSetSubject is not null;
                bool useWorkItemSubject = workItemSubjectTags is not null || workItemSubject is not null;

                if( (useWorkSetSubject || useWorkItemSubject) && 
                    (workSetSubject is null || workItemSubject is null))
                        (workSetSubject, workItemSubject) = 
                            await _workSetAndItemFinder.Find(workSetSubjectTags, workItemSubjectTags);

                if ((workSet is null || workItem is null))
                    (workSet, workItem) =
                        await _workSetAndItemFinder.Find(workSetHostTags, workItemHostTags);

                workSet.HasValue.Guarantees().IsTrue();
                workItem.HasValue.Guarantees().IsTrue();

                var (id, template) = await HelpCreateKPIInstance(
                    origin, null, templateName,
                    workSet!.Value, workItem!.Value,
                    userId,
                    userSubject,
                    workSetSubject,
                    workItemSubject,
                    useWorkSetSubject,
                    useWorkItemSubject,
                    initialTags, trx!,
                    seal, "event",
                    null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                return id;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    
    }
    #endregion

    #region evaluation
    public async Task EvaluateKPI(
        KPIInstance instance,
        AppEventOrigin? origin,
        bool seal = false,
        DateTime? endTime = null,
        ITransactionContext? trx = null)
    {
        using(PerfTrack.Stopwatch($"{nameof(EvaluateKPI)} - {instance.Template}"))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var template = _content.GetContentByName<KPITemplate>(instance.Template)!;
                template.Guarantees($"content missing - KPITemplate {instance.Template}").IsNotNull();

                var kpiData = await _kpiEvaluator.ComputeKPI(template, instance, endTime);

                if(kpiData is null)
                {
                    return;
                }

                if (kpiData.Samples.Any())
                {
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);

                    _sink.BeginBatch(trx!);


                    await _kpiData.CreateAsync(kpiData);

                    var entity = new EntityWrapping<KPIData>
                    {
                        Payload = kpiData,
                        CreatedDate = DateTime.UtcNow,
                        Creator = Constants.BuiltIn.SystemUser,
                        EntityType = nameof(KPIData),
                        HostWorkItem = instance.HostWorkItem,
                        HostWorkSet = instance.HostWorkSet,
                        Id = kpiData.Id,
                        LastModifier = Constants.BuiltIn.SystemUser,
                        Tags = instance.Tags,
                        Template = instance.Template,
                        UpdatedDate = DateTime.UtcNow
                    };

                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.kpi_eval";

                    await _sink.Enqueue(
                        origin,
                        topic,
                        null,
                        entity,
                        null, null,
                        seal);


                    if (kpiData.Signals.Any())
                    {
                        foreach (var signal in kpiData.Signals)
                        {
                            var sigEnt = new EntityWrapping<KPISignal>
                            {
                                Payload = signal,
                                CreatedDate = DateTime.UtcNow,
                                Creator = Constants.BuiltIn.SystemUser,
                                EntityType = nameof(KPIData),
                                HostWorkItem = instance.HostWorkItem,
                                HostWorkSet = instance.HostWorkSet,
                                Id = kpiData.Id,
                                LastModifier = Constants.BuiltIn.SystemUser,
                                Tags = instance.Tags,
                                Template = instance.Template,
                                UpdatedDate = DateTime.UtcNow
                            };

                            topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.kpi_signal.{signal.SignalName.ToLowerInvariant()}";

                            await _sink.Enqueue(
                                origin,
                                topic,
                                null,
                                sigEnt,
                                null,
                                null,
                                seal);
                        }
                    }

                    await _sink.CommitBatch();
                }


                if (isSelfContained)
                    await trx!.CommitAsync();


            }
            catch(KPIInsufficientDataException kex)
            {
                // forgivable -- note and ignore
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Warning, kex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }

        }
        

    }
    
    public async Task EvaluateMatchingKPIs(
        string eventTopic,
        AppEventOrigin? origin,
        bool seal = false,
        DateTime? endTime = null,
        ITransactionContext? trx = null)
    {
        using(PerfTrack.Stopwatch(nameof(EvaluateMatchingKPIs)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (matches, _) = await _instances.GetAllAsync(it => it.EventTopic == eventTopic);

                var work = new List<Task>();
                foreach (var match in matches)
                {
                    try
                    {
                        work.Add(EvaluateKPI(match, origin, seal, endTime, trx));
                    }
                    catch (Exception ex)
                    {
                        _alerts.RaiseAlert(ApplicationAlertKind.Defect,
                            LogLevel.Error, ex.TraceInformation());
                    }
                }

                await Task.WhenAll(work);



            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }
    #endregion

    #region deletion
    public async Task HelpDeleteKPIInstance(
        Guid id,
        AppEventOrigin? origin,
        string? action,
        Guid? userId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {
        using(PerfTrack.Stopwatch(nameof(HelpDeleteKPIInstance)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (existing, _) = await _instances.LoadAsync(id);
                if (existing is null)
                    return;

                await _instances.DeleteAsync(existing);

                // data are intentionally left alone. Groom it if you must.

                await _comments.DeleteFilterAsync(trx!, c => c.HostEntity == id);

                await _tagger.Untag(trx!, existing, _instances, existing.Tags);

                

                _sink.BeginBatch(trx!);
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(existing.HostWorkSet!.Value, existing.HostWorkItem!.Value!);
                var topic = $"{workSetTemplateName}.{workItemTemplateName}.{existing.Template}.{context}.kpi_delete_instance";
                await _sink.Enqueue(origin, topic, action, existing, userId, eventTags, seal);
                await _sink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }

        }
    }


    public async Task EventDeleteKPIInstance(
        Guid id,
        AppEventOrigin origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteKPIInstance)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteKPIInstance(id, origin, null, null, trx!, seal, "event", null);

                if (isSelfContained)
                    await trx!.CommitAsync();
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }

    }

    public async Task EventDeleteWorkItemKPIInstances(
        Guid workItem,
        AppEventOrigin? origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using(PerfTrack.Stopwatch(nameof(EventDeleteWorkItemKPIInstances)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (matching, _) = await _instances.GetAllAsync(fi => fi.HostWorkItem == workItem);
                if (!matching.Any())
                    return;

                var work = new List<Task>();
                foreach (var item in matching)
                {
                    work.Add(HelpDeleteKPIInstance(item.Id, origin, null, null, trx!, seal, "event", null));
                }
                await Task.WhenAll(work);

                if (isSelfContained)
                    await trx!.CommitAsync();
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task EventDeleteWorkItemKPIInstance(
        Guid workItem,
        Uri entityUri,
        AppEventOrigin? origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteWorkItemKPIInstances)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                bool wantsTemplate = entityUri.Segments.Any(it => it.ToLowerInvariant() == "template");
                if (wantsTemplate)
                    return;

                var id = new Guid(entityUri.Segments.Last());

                var (matching, _) = await _instances.LoadAsync(id);
                if (matching is null || matching.HostWorkItem != workItem)
                    return;

                await HelpDeleteKPIInstance(id, origin, null, null, trx!, seal, "event", null);
                
                

                if (isSelfContained)
                    await trx!.CommitAsync();
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task ActionDeleteKPIInstance(
       Guid id,
       string action,
       Guid userId,
       bool seal = false,
       ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionDeleteKPIInstance)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteKPIInstance(id, null, action, userId, trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }


    }


    public async Task GroomKPIData(string templateName)
    {
        using(PerfTrack.Stopwatch(nameof(GroomKPIData)))
        {
            try
            {
                var template = _content.GetContentByName<KPITemplate>(templateName)!;
                template.Guarantees().IsNotNull();

                if(template.DataGroomingTimeFrame is not null)
                {

                    var cutOff = template.DataGroomingTimeFrame.BackFrom(DateTime.UtcNow);
                    await _kpiData.DeleteFilterAsync(
                        d => d.SampleTime < cutOff && d.KPITemplateName == templateName);
                }

            } catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
            }

        }
    }
    #endregion

    #region retrieval
    public IList<KPITemplateViewModel> GetCreatableKPIs()
    {
        return _content.GetAllContent<KPITemplate>()
            .Where(t => t.AllowUserCreate)
            .Select(t => new KPITemplateViewModel
            {
                AllowUserCreate = t.AllowUserCreate,
                AllowUserDelete = t.AllowUserDelete,
                IconClass = t.IconClass,   
                Name = t.Name,
                Tags = t.Tags,
                Title = _terms.ReplaceTerms(t.Title)
            })
            .ToList();

    }

    public KPITemplate? GetRawKPITemplate(string templateName)
    {
        return _content.GetContentByName<KPITemplate>(templateName);
    }

    public KPITemplateViewModel? GetKPITemplateVM(string templateName)
    {
        KPITemplateViewModel? result = null!;
        var template = GetRawKPITemplate(templateName);
        if (template is not null)
            result = new KPITemplateViewModel
            {
                AllowUserCreate = template.AllowUserCreate,
                AllowUserDelete = template.AllowUserDelete,
                IconClass = template.IconClass,
                Name = template.Name,
                Tags = template.Tags,
                Title = _terms.ReplaceTerms(template.Title)
            };
        return result;
    }

    public async Task<IList<KPIViewModel>> GetKPIInstances(
        Guid workItem, string tzId, 
        DateTime? begin = null, DateTime? end = null)
    {
        using (PerfTrack.Stopwatch(nameof(GetKPIInstances)))
        {
            var (items, _) = await _instances.GetAllAsync(fi => fi.HostWorkItem == workItem);
            
            var work = new List<Task<KPIViewModel>>();
            foreach (var item in items)
            {
                var template = _content.GetContentByName<KPITemplate>(item.Template)!;
                template.Guarantees().IsNotNull();
                work.Add(KPIViewModel.Create(template, item,
                    _terms, _kpiData, _userCache, _workSets, _workItems,
                    begin, end, tzId));
            }

            await Task.WhenAll(work);

            return work.Select(k=>k.Result).ToList();

        }

    }

    public async Task<KPIInstance?> GetRawKPIInstance(Guid id)
    {
        var (instance,_) = await _instances.LoadAsync(id);
        return instance;
    }

    public async Task<List<KPIInstance>> GetRawKPIInstances()
    {
        var (instances, _) = await _instances.GetAllAsync(kpi=>true);
        return instances;
    }

    public async Task<KPIViewModel?> GetKPIInstanceVM(Guid id, string? tzid, DateTime? begin, DateTime? end)
    {
        KPIViewModel? retval = null!;
        var instance = await GetRawKPIInstance(id);
        if(instance is not null)
        {
            var template = GetRawKPITemplate(instance.Template)!;
            template.Guarantees().IsNotNull();
            retval = await KPIViewModel.Create(template, instance, 
                _terms, _kpiData, _userCache, _workSets, _workItems,
                begin, end, tzid);
        }

        return retval;
    }

    public async Task<Uri> GetKPIInstanceRef(Guid id)
    {

        var instance = (await GetRawKPIInstance(id))!;
        instance.Guarantees().IsNotNull();
        return instance.MakeReference();
    }

    public async Task<List<EntitySummary>> GetTaggedKPIInstanceRefs(Guid workItem, IEnumerable<string> tags)
    {
        using(PerfTrack.Stopwatch(nameof(GetTaggedKPIInstanceRefs)))
        {
            var readyTags = TagUtil.MakeTags(tags);
            var (instances, _) = await _instances.
                GetAllAsync(fi => readyTags.Any(rt => fi.HostWorkItem == workItem && fi.Tags.Contains(rt)));

            var retval = instances.Select(it => new EntitySummary
            {
                EntityTemplate = it.Template,
                EntityTags = it.Tags,
                EntityType = it.EntityType,
                Uri = it.MakeReference(false, true)
            }).ToList();
            return retval;
        }
    }

    #endregion

}
