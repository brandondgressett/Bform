using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Platform.WorkItems;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using LinqKit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkSets;

/// <summary>
/// WorkSetLogic manages CRUD opertations for work sets, building dashboards, and managing ownership over worksets. 
///     -References:
///         >AcceptWorkSetInstanceContent.cs
///         >BuildDashboardJob.cs
///         >WorksetLoaderModule.cs
///         >Rule Actions
///     -Functions:
///         >CRUD operations
///         >Work set onership management
///         >Building dashboards
/// </summary>
public class WorkSetLogic
{
    #region fields
    private readonly IRepository<DashboardCandidate> _dashItems;
    private readonly IRepository<WorkSet> _workSets;
    private readonly IRepository<WorkSetMember> _members;
    private readonly IApplicationTerms _terms;
    private readonly IApplicationPlatformContent _content;
    private readonly IDataEnvironment _dataEnvironment;
    private readonly AppEventSink _eventSink;
    private readonly TemplateNamesCache _tnCache;
    private readonly IApplicationAlert _alerts;
    private readonly UserInformationCache _userCache;
    private readonly Tagger _tagger;
    private readonly BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic _scheduler;
    private readonly WorkItemLogic _workItems;
    private readonly EntityReferenceLoader _loader;

    #endregion

    #region constructor
    public WorkSetLogic(
        IApplicationAlert alerts,
        IApplicationPlatformContent content,
        IDataEnvironment env,
        AppEventSink sink,
        TemplateNamesCache tnCache,
        IRepository<DashboardCandidate> dashItems,
        IRepository<WorkSet> workSets,
        IRepository<WorkSetMember> members,
        IApplicationTerms terms,
        UserInformationCache userCache,
        Tagger tagger,
        BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic scheduler,
        WorkItemLogic workItems,
        EntityReferenceLoader loader)
    {
        _alerts = alerts;
        _content = content;
        _dataEnvironment = env;
        _eventSink = sink;
        _terms = terms;
        _dashItems = dashItems;
        _workSets = workSets;
        _members = members;
        _tnCache = tnCache;
        _userCache = userCache;
        _tagger = tagger;
        _scheduler = scheduler;       
        _workItems = workItems;
        _loader = loader;
    }
    #endregion

    #region creation

    public async Task<(WorkSetTemplate, WorkSet)> HelpCreateWorkSet(
        AppEventOrigin? origin,
        string? action,
        string templateName,
        string title,
        string description,
        Guid? userId,
        Guid? ownerId,
        IEnumerable<string>? initialTags,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {

        var id = Guid.NewGuid();
        var template = _content.GetContentByName<WorkSetTemplate>(templateName)!;
        template.Guarantees().IsNotNull();

        if (action is not null)
        {
            template.StartingInteractivityState.Guarantees().IsEqualTo(WorkSetInteractivityState.Open);
            template.Management.Guarantees().IsEqualTo(WorkSetManagement.UserManaged);
        }

        var readyTags = TagUtil.MakeTags(initialTags.EmptyIfNull());

        if (ownerId is null)
            ownerId = Constants.BuiltIn.SystemUser; // users in the ProjectManager or SiteManager role can reassign the owner in this case.

        var workSet = new WorkSet
        {
            Id = id,
            Version = 0,
            Template = templateName,
            Title = title,
            Description = description,
            ProjectOwner = ownerId,
            CreatedDate = DateTime.UtcNow,
            Creator = userId,
            LastModifier = userId,
            UpdatedDate = DateTime.UtcNow,
            HostWorkSet = Constants.BuiltIn.SystemWorkSet,
            HostWorkItem = Constants.BuiltIn.SystemWorkItem,
            InteractivityState = template.StartingInteractivityState,
            Tags = new(readyTags),
        };

        var scheduleId = await _scheduler.ScheduleJobAsync<BuildDashboardJob>(
            template.DashboardSchedule, 
            JObject.FromObject(workSet));

        workSet.AttachedSchedules.Add(scheduleId.JobId); //CAG Change need to set schedule id correctly, maybe even change schedule job to give the id of the job as a string

        await _workSets.CreateAsync(trx!, workSet);

        var work = new List<Task>();
        foreach (var tag in readyTags)
            work.Add(_tagger.CountTags(workSet, trx, 1, tag));
        await Task.WhenAll(work);

        // create all fixed work items
        foreach(var wi in template.WorkItemCreationTemplates)
        {
            if(wi.CreateOnInitialization)
            {
                // use work item logic to create work items assigned to this workset, provide initial tags
                await _workItems.HelpCreateWorkItem(
                    origin, action, wi.TemplateName, wi.Title, null, null,
                    null, userId, null, null, null, null, null, null, userId,
                    id, wi.Tags, trx, seal, context, eventTags);
            }
        }

        _eventSink.BeginBatch(trx!);
        var topic = $"{templateName}.{context}.workset.create";
        await _eventSink.Enqueue(origin, topic, action, workSet, userId, eventTags, seal);
        topic = $"{templateName}.{context}.workset.join_dashboard";
        await _eventSink.Enqueue(origin, topic, action, workSet, userId, eventTags, seal);
        
        DateTime initialWait = DateTime.UtcNow.AddSeconds(template.DashboardBuildDeferralSeconds);
        topic = $"{templateName}.{context}.workset.build_dashboard";
        await _eventSink.Enqueue(origin, topic, action, workSet, userId, eventTags, seal, initialWait);

        await _eventSink.CommitBatch();
        return (template, workSet);
    }

    public async Task<WorkSetSummaryViewModel> ActionCreateWorkSet(
        string action,
        string templateName,
        string title,
        string description,
        Guid userId,
        Guid ownerId,
        string tzId,
        IEnumerable<string> initialTags,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionCreateWorkSet)))
        {
            bool isSelfContained = trx is null;
            if(isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (template, workSet) =
                    await HelpCreateWorkSet(null,
                        action, templateName, title, description, userId, ownerId, initialTags,
                        trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkSetSummaryViewModel.Create(workSet, template, _userCache);

                return retval;

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

    public async Task<WorkSet> EventCreateWorkSet(
        AppEventOrigin origin,
        string templateName,
        string title,
        string description,
        Guid? userId,
        Guid? ownerId,
        IEnumerable<string>? initialTags,
        bool seal = false, 
        ITransactionContext? trx = null)
    {
        using(PerfTrack.Stopwatch(nameof(EventCreateWorkSet)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (template, workSet) =
                    await HelpCreateWorkSet(origin,
                        null, templateName, title, description, userId, ownerId, initialTags,
                        trx!, seal, "event", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                return workSet;

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

    #region dashboard build
    public async Task EnrollEntityIntoDashboard(
        ITransactionContext? ctx,
        DashboardCandidate candidate)
    {
        using (PerfTrack.Stopwatch(nameof(EnrollEntityIntoDashboard)))
        {
            try
            {
                await _dashItems.CreateAsync(ctx, candidate);
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());

                throw;
            }
        }
    }


    public async Task BuildWorkSetDashboard(WorkSet ws)
    {
        using (PerfTrack.Stopwatch(nameof(BuildWorkSetDashboard)))
        {
            try
            {
                // groom out all too old
                var template = _content.GetContentByName<WorkSetTemplate>(ws.Template)!;
                template.Guarantees().IsNotNull();
                var groomingFrame = template.OldToGroom;
                var groomDate = groomingFrame.BackFrom(DateTime.UtcNow);

                var (old, rc) = await _dashItems.GetAllAsync(it => it.Created < groomDate);
                if(old.Any())
                    await _dashItems.DeleteBatchAsync(old.Select(it => it.Id));

                // clear current winners
                List<DashboardCandidate> workingSet;
                (workingSet, rc) = await _dashItems.GetAllAsync(it =>
                    it.WorkSet == ws.Id && it.IsWinner);

                var work = new List<Task>();
                if (workingSet.EmptyIfNull().Any())
                {
                    
                    foreach (var c in workingSet)
                    {
                        c.IsWinner = false;
                        work.Add(_dashItems.UpdateIgnoreVersionAsync(c));
                    }

                    await Task.WhenAll(work);
                }

                // initial view data
                work.Clear();
                foreach(var init in template.ViewDataInitialization)
                    work.Add(_workItems.EnrollIntoDashboardByTags(ws.Id, init));
                await Task.WhenAll(work);

                // for each required view, find and mark the desired winners
                var queries = FindQueries(template.View);

                var foundData = new Dictionary<Guid,DashboardCandidate>();
                foreach(var query in queries)
                {
                    var predicate = PredicateBuilder.New<DashboardCandidate>();

                    predicate = predicate.And(dc => dc.WorkSet == ws.Id);

                    if (query.ScoreThreshold.HasValue)
                        predicate = predicate.And(dc => dc.Score > query.ScoreThreshold.Value);

                    if(query.Grouping is not null)
                        predicate = predicate.And(dc => dc.Grouping == query.Grouping);

                    if(query.Recency is not null)
                    {
                        var cutDate = query.Recency.BackFrom(DateTime.UtcNow);
                        predicate = predicate.And(dc => dc.Created > cutDate);
                    }

                    if(query.EntityType is not null)
                        predicate = predicate.And(dc => dc.EntityType == query.EntityType); 

                    if(query.TemplateName is not null)
                        predicate = predicate.And(dc=>dc.TemplateName == query.TemplateName);

                    if(query.FindAllTags.Any())
                        predicate = predicate.And(dc => query.FindAllTags.All(tg => dc.Tags.Contains(tg)));
                    
                    if(query.FindAnyTags.Any())
                        predicate = predicate.And(dc => query.FindAllTags.Any(tg => dc.Tags.Contains(tg)));

                    
                    switch (query.Ordering)
                    {
                        case ViewDataOrdering.Score:
                            (workingSet, rc) = await _dashItems.GetAllOrderedAsync<double>
                                (dc => dc.Score, true,
                                predicate);
                            break;

                        case ViewDataOrdering.Order:
                            (workingSet, rc) = await _dashItems.GetAllOrderedAsync<int>
                                (dc => dc.DescendingOrder, true,
                                predicate);
                            break;

                        case ViewDataOrdering.Recency:
                            (workingSet, rc) = await _dashItems.GetAllOrderedAsync<DateTime>
                                (dc => dc.Created, true,
                                predicate);
                            break;

                        case ViewDataOrdering.Grouping:
                            
                            (workingSet, rc) = await _dashItems.GetAllOrderedAsync<string>
                                (dc => dc.Grouping ?? "", true,
                                predicate);
                            break;

                    }

                    if(query.Limit.HasValue)
                    {
                        workingSet = workingSet.Take(query.Limit.Value).ToList();
                    }

                    foreach(var dc in workingSet)
                    {
                        if(!foundData.TryGetValue(dc.Id, out DashboardCandidate? item))
                        {
                            foundData.Add(dc.Id, dc);
                            item = dc;
                        }

                        item.IsWinner = true;

                        foreach(var mt in query.AddMetaTags)
                            if(!item.MetaTags.Contains(mt))
                                item.MetaTags.Add(mt);
                    }


                }

                work.Clear();
                foreach (var dc in foundData.Values)
                    if(dc.IsWinner)
                        work.Add(_dashItems.UpdateIgnoreVersionAsync(dc));
                await Task.WhenAll(work);

            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
             
                throw;
            }
        }


        static List<ViewDataQuery> FindQueries(List<ViewRowDef> rows)
        {
            var retval = new List<ViewDataQuery>();

            foreach (var row in rows)
            {
                if(row is ViewSeveralRowDef)
                {
                    var vwr = (row as ViewSeveralRowDef)!;
                    retval.Add(vwr.RowQuery);
                } else
                if( row is ViewColumnsRowDef)
                {
                    var vwr = (row as ViewColumnsRowDef)!;
                    var columns = vwr.Columns;
                    foreach (var col in columns)
                    {
                        if (col is ViewNestedGridDef)
                        {
                            var vwc = (col as ViewNestedGridDef)!;
                            var found = FindQueries(vwc.NestedGrid);
                            retval.AddRange(found);
                        }
                        else
                        if (col is ViewPerColumnDef)
                        {
                            var vwc = (col as ViewPerColumnDef)!;
                            retval.Add(vwc.ColumnQuery);
                        }
                    }
                }
            }

            return retval;

            
        }

    }

    public async Task<List<DashboardCandidate>> GetDashboardWinners(Guid workSetId)
    {
        var (data, _) = await _dashItems.GetAllAsync(dc => dc.WorkSet == workSetId && dc.IsWinner);
        return data;
    }

    


    #endregion


    #region edit metadata

    public async Task<(WorkSet,WorkSetTemplate)> HelpUpdateWorkSetMetadata
        (
            AppEventOrigin? origin,
            string? action,
            Guid workSetId,
            string? title,
            string? description,
            WorkSetInteractivityState? interactivityState,
            IEnumerable<string>? setTags,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {

        var (ws,rc) = await _workSets.LoadAsync(workSetId)!;
        ws.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkSetTemplate>(ws.Template)!;
        template.Guarantees().IsNotNull();


        if (action is not null)
        {
            bool allowed = (userId == ws.ProjectOwner) || ws.InteractivityState == WorkSetInteractivityState.Open;
            allowed.Guarantees().IsTrue();
            template.Management.Guarantees().IsEqualTo(WorkSetManagement.UserManaged);
        }

        if(title is not null) ws.Title = title;
        if (description is not null) ws.Description = description;

        if(interactivityState is not null && interactivityState != ws.InteractivityState)
        {
            if(interactivityState == WorkSetInteractivityState.Open)
            {
                ws.LockedDate = DateTime.MaxValue;
            } else 
            if(interactivityState == WorkSetInteractivityState.ReadOnly)
            {
                ws.LockedDate = DateTime.UtcNow;
            }

            ws.InteractivityState = interactivityState.Value;
        }

        ws.UpdatedDate = DateTime.UtcNow;
        if (userId is not null)
            ws.LastModifier = userId;
        else
            ws.LastModifier = Constants.BuiltIn.SystemUser;

        if(setTags is not null && setTags.Any())
        {
            var readyTags = TagUtil.MakeTags(setTags);
            await _tagger.ReconcileTags(trx, ws, readyTags);
        }

        await _workSets.UpdateAsync((ws, rc));

        _eventSink.BeginBatch(trx!);
        var topic = $"{ws.Template}.{context}.workset.update_metadata";
        await _eventSink.Enqueue(origin, topic, action, ws, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (ws,template);

    }

    public async Task<WorkSetSummaryViewModel> ActionUpdateMetadata
        (
            string action,
            Guid workSetId,
            string? title,
            string? description,
            WorkSetInteractivityState? interactivityState,
            IEnumerable<string>? setTags,
            Guid userId,
            string tzId,
            bool seal = false,
            ITransactionContext? trx = null
        )
    {
        using(PerfTrack.Stopwatch(nameof(ActionUpdateMetadata)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (ws,template) = await HelpUpdateWorkSetMetadata(null,
                    action, workSetId, title, description, interactivityState, setTags, userId, trx!, seal, "action");
                
                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkSetSummaryViewModel.Create(ws, template, _userCache);
                return retval;
                
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

    public async Task<WorkSet> EventUpdateMetadata(
        AppEventOrigin origin,
        Guid workSetId,
        string? title,
        string? description,
        WorkSetInteractivityState? interactivityState,
        IEnumerable<string>? setTags,
        Guid? userId,
        IEnumerable<string>? eventTags,
        bool seal = false,
        ITransactionContext? trx = null
        )
    {
        using(PerfTrack.Stopwatch(nameof(EventUpdateMetadata)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (workSet,template) = await HelpUpdateWorkSetMetadata(origin, null, workSetId,
                    title, description, interactivityState, setTags, userId ?? Constants.BuiltIn.SystemUser,
                    trx!, seal, "event", eventTags);

                if (isSelfContained)
                    await trx!.CommitAsync();

                return workSet;
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

    #region membership

    public async Task<List<WorkSet>> SysFindWorkSetsForMember(Guid userId)
    {
        using (PerfTrack.Stopwatch(nameof(FindWorkSetsForMember)))
        {
            var universalTemplates = _content.GetAllContent<WorkSetTemplate>().Where(t => t.IsEveryoneAMember);
            var (universals, ___) = await _workSets.GetAllAsync(ws => universalTemplates.Any(ut => ws.Template == ut.Name));
            var (directMembers, _) = await _members.GetAllAsync(it => it.UserId == userId);
            var directIds = directMembers.Select(it => it.WorkSetId).Distinct().ToList();
            var (directs, __) = await _workSets.LoadManyAsync(directIds);
            var all = universals.Concat(directs).ToList();
            return all;
        }
    }

    public async Task<List<WorkSetSummaryViewModel>> FindWorkSetsForMember(Guid userId, string tzId)
    {
        using (PerfTrack.Stopwatch(nameof(FindWorkSetsForMember)))
        {
            var all = await SysFindWorkSetsForMember(userId);
            var retval = new List<WorkSetSummaryViewModel>();
            foreach (var ws in all)
            {
                var template = _content.GetContentByName<WorkSetTemplate>(ws.Template)!;
                template.Guarantees().IsNotNull();
                retval.Add(await WorkSetSummaryViewModel.Create(ws, template, _userCache));
            }

            return retval;
        }
    }

    // get members
    public async Task<List<WorkSetMember>> EventGetMembersAsync(Guid workSetId, int page)
    {
        var (data, rc) = await _members.GetPageAsync(page, wsm=>wsm.WorkSetId == workSetId);
        return data;
    }

    public async Task<List<WorkSetMemberViewModel>> ActionGetMembersAsync(Guid workSetId, int page)
    {
        var data = await EventGetMembersAsync(workSetId, page);
        var work = new List<Task<WorkSetMemberViewModel>>();
        foreach (var item in data)
            work.Add(WorkSetMemberViewModel.Create(item, _workSets, _userCache, _alerts));

        await Task.WhenAll(work);
        return work.Select(it => it.Result).Where(it => it is not null).ToList();
    }
        
    public async Task HelpAddMember(//CAG What is happening?!?!? We Don't add a member
        AppEventOrigin? origin,
        string? action,
        Guid workSetId,
        Guid memberId,
        Guid? userId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {
        var (ws, _) = await _workSets.LoadAsync(workSetId)!;
        ws.Guarantees().IsNotNull();
                
        var (existing, rc) = await _members.GetOneAsync(it => it.WorkSetId == workSetId && it.UserId == memberId);
        if (existing is not null)
            return;
        else
        {
            WorkSetMember member = new WorkSetMember();

            member.UserId = memberId;
            member.WorkSetId = workSetId;

            _members.Create(member);
        }

        _eventSink.BeginBatch(trx);
        var topic = $"{ws.Template}.{context}.workset.add_member";
        await _eventSink.Enqueue(origin, topic, action, ws, userId, eventTags, seal);
        await _eventSink.CommitBatch(); 

    }

    public async Task ActionAddMember(
        string action,
        Guid workSetId,
        Guid memberId,
        Guid userId,
        bool seal,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionAddMember)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpAddMember(null, action, workSetId, memberId, userId, trx!, seal, "action");

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

    public async Task EventAddMember(
        AppEventOrigin origin,
        Guid workSetId,
        Guid memberId,
        Guid? userId,
        IEnumerable<string>? eventTags,
        bool seal= false,
        ITransactionContext? trx = null)
    {

        using(PerfTrack.Stopwatch(nameof(EventAddMember)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpAddMember(origin, null, workSetId, memberId, userId, trx!, seal, "event", eventTags);

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

    public async Task HelpRemoveMember(//CAG Can't Delete what we can't set
        AppEventOrigin? origin,
        string? action,
        Guid workSetId,
        Guid memberId,
        Guid? userId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {
        var (ws, _) = await _workSets.LoadAsync(workSetId)!;
        ws.Guarantees().IsNotNull();

        await _members.DeleteFilterAsync(it => it.WorkSetId == workSetId && it.UserId == memberId);

        _eventSink.BeginBatch(trx);
        var topic = $"{ws.Template}.{context}.workset.remove_member";
        await _eventSink.Enqueue(origin, topic, action, ws, userId, eventTags, seal);
        await _eventSink.CommitBatch();


    }

    public async Task ActionRemoveMember(
        string action,
        Guid workSetId,
        Guid memberId,
        Guid userId,
        bool seal,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionRemoveMember)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpRemoveMember(null, action, workSetId, memberId, userId, trx!, seal, "action");

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

    public async Task EventRemoveMember(
        AppEventOrigin origin,
        Guid workSetId,
        Guid memberId,
        Guid? userId,
        IEnumerable<string>? eventTags,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventRemoveMember)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpRemoveMember(origin, null, workSetId, memberId, userId, trx!, seal, "event", eventTags);

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




    #endregion

    #region ownership
    public async Task HelpSetOwner(
        AppEventOrigin? origin,
        string? action,
        Guid workSetId,
        Guid owner,
        Guid? userId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags)
    {
        // let's assume user role has already been checked!

        var (ws, rc) = await _workSets.LoadAsync(workSetId)!;
        ws.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkSetTemplate>(ws.Template)!;
        template.Guarantees().IsNotNull();

        if (owner == ws.ProjectOwner)
            return;
        else
            ws.ProjectOwner = owner;

        var userInfo = _userCache.Fetch(owner);
        userInfo.Guarantees().IsNotNull();

        ws.UpdatedDate = DateTime.UtcNow;
        if (userId is not null)
            ws.LastModifier = userId;
        else
            ws.LastModifier = Constants.BuiltIn.SystemUser;

        await _workSets.UpdateAsync((ws, rc));

        _eventSink.BeginBatch(trx!);
        var topic = $"{ws.Template}.{context}.workset.update_owner";
        await _eventSink.Enqueue(origin, topic, action, ws, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        

    }

    public async Task ActionSetOwner(
        string action,
        Guid workSetId,
        Guid owner,
        Guid userId,
        bool seal,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionSetOwner)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpSetOwner(null, action, workSetId, owner, userId, trx!, seal, "action", null);

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

    public async Task EventSetOwner(
        AppEventOrigin origin,
        Guid workSetId,
        Guid owner,
        Guid? userId,
        IEnumerable<string>? eventTags,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventSetOwner)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpSetOwner(origin, null, workSetId, owner, userId, trx!, seal, "event", eventTags);

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


    #endregion

    #region work item templates

    public async Task<List<CreatableWorkItem>> GetCreatableWorkItems(Guid workSetId)
    {
        var (ws, rc) = await _workSets.LoadAsync(workSetId)!;
        ws.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkSetTemplate>(ws.Template)!;
        template.Guarantees().IsNotNull();

        return template.WorkItemCreationTemplates;
    }

    public async Task<List<CreatableWorkItem>> GetUserCreatableWorkItems(Guid workSetId)
    {
        var (ws, rc) = await _workSets.LoadAsync(workSetId)!;
        ws.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkSetTemplate>(ws.Template)!;
        template.Guarantees().IsNotNull();

        return template.WorkItemCreationTemplates.Where(it=>it.UserCreatable).ToList();
    }

    #endregion

    #region deletion
    private async Task HelpDeleteWorkSet(
        Guid id,
        AppEventOrigin? origin,
        string? action,
        Guid? userId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags)
    {
        using(PerfTrack.Stopwatch(nameof(HelpDeleteWorkSet)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (existing, rc) = await _workSets.LoadAsync(id);
                if (existing is null)
                    return;

                await _members.DeleteFilterAsync(trx!, member => member.WorkSetId == existing.Id);
                
                // TODO: Delete all workitems for this workset

                var work = new List<Task>();
                foreach (var tag in existing.Tags)
                    work.Add(_tagger.CountTags(existing, trx!, -1, tag));
                await Task.WhenAll(work);

                if (existing.AttachedSchedules.EmptyIfNull().Any())
                {
                    foreach (var schedule in existing.AttachedSchedules)
                    {
                        await _scheduler.DescheduleAsync(schedule);
                    }
                }

                await _workSets.DeleteAsync(trx!, (existing, rc));

                _eventSink.BeginBatch(trx!);
                var workSetTemplateName = existing.Template;
                var topic = $"{workSetTemplateName}.{context}.workset_delete_instance";
                await _eventSink.Enqueue(origin, topic, action, existing, userId, eventTags, seal);
                await _eventSink.CommitBatch();

                if (isSelfContained)
                    await trx!.CommitAsync();

            } catch(Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if (isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }


        }

    }

    public async Task EventDeleteWorkSet(
        Guid id,
        AppEventOrigin origin,
        bool seal = false,
        ITransactionContext? trx = null,
        IEnumerable<string>? eventTags = null)
    {
        using(PerfTrack.Stopwatch(nameof(EventDeleteWorkSet)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteWorkSet(id, origin, null, null, trx!, seal, "event", eventTags);

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

    public async Task ActionDeleteWorkSet(
        Guid id,
        string action,
        Guid userId,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionDeleteWorkSet)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteWorkSet(id, null, action, userId, trx!, seal, "action", null);

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

    #endregion

    #region retrieval

    public async Task<WorkSetViewModel?> GetWorkSetViewModel(WorkSet ws)
    {
        var template = _content.GetContentByName<WorkSetTemplate>(ws.Template)!;
        template.Guarantees().IsNotNull();
        var winners = await GetDashboardWinners(ws.Id);
        return await WorkSetViewModel.Create(ws, template, winners, _loader, _userCache);
    }

    public async Task<WorkSetViewModel?> GetWorkSetViewModel(Guid workSetId)
    {
        var (ws,_) = await _workSets.LoadAsync(workSetId)!;
        if (ws is null)
            return null;
        return await GetWorkSetViewModel(ws);
    }

    public async Task<List<WorkSetViewModel>> GetMemberWorkSetViewModels(Guid userId)
    {
        var memberWorkSets = await SysFindWorkSetsForMember(userId);
        var work = new List<Task<WorkSetViewModel?>>();
        foreach (var ws in memberWorkSets)
            work.Add(GetWorkSetViewModel(ws));
        await Task.WhenAll(work);
        return work
            .Select(it=>it.Result!)
            .Where(it=>it is not null)
            .ToList();
    }

    public List<WorkSetTemplateViewModel> GetCreatableTemplates()
    {
        var templates = _content.GetAllContent<WorkSetTemplate>()
            .Where(it => 
                       it.IsUserCreatable &&
                       it.IsVisibleToUsers &&
                       it.Management == WorkSetManagement.UserManaged &&
                       it.StartingInteractivityState == WorkSetInteractivityState.Open)
            .Select(it => WorkSetTemplateViewModel.Create(it))
            .ToList();
        return templates;
    }

    #endregion



}
