using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Comments;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.ManagedFiles;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Platform.WorkSets;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using LinqKit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.WorkItems;

/// <summary>
/// WorkItemLogic manages CRUD operations for work items and also manages dashboard content creation
///     -References:
///         >AcceptWorkItemInstanceContent.cs
///         >WorkItemGroomingService.cs
///         >WorkItemLoaderModule.cs
///         >WorkSetLogic.cs
///         >Rule Actions
///     -Functions:
///         >CRUD opertations of various kinds 
///         >Dashboard content creation
///         >Link and bookmark creation
/// </summary>
public class WorkItemLogic
{
    #region fields
    private readonly IApplicationAlert _alerts;
    private readonly ILogger<WorkItemLogic> _logger;
    private readonly IDataEnvironment _dataEnvironment;
    private readonly IApplicationPlatformContent _content;
    private readonly EntityReferenceLoader _loader;
    private readonly Tagger _tagger;
    private readonly IApplicationTerms _terms;
    private readonly AppEventSink _eventSink;
    private readonly CommentsLogic _commentsLogic;
    private readonly IRepository<Comment> _comments;
    private readonly UserInformationCache _userCache;
    private readonly TemplateNamesCache _tnCache;
    private readonly IRepository<ManagedFileInstance> _managedFiles;
    private readonly ManagedFileLogic _managedFileLogic;
    private readonly BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic _scheduler;
    private readonly IRepository<WorkItem> _workItems;
    private readonly IRepository<DashboardCandidate> _dashItems;
    private readonly IEnumerable<IEntityInstanceLogic> _entityCreators;

    #endregion

    #region ctor
    public WorkItemLogic(
        IApplicationAlert alerts,
        ILogger<WorkItemLogic> logger,
        IDataEnvironment dataEnvironment,
        IRepository<WorkItem> workItems,
        TemplateNamesCache tnCache,
        CommentsLogic commentsLogic,
        IRepository<Comment> comments,
        IRepository<ManagedFileInstance> files,
        IApplicationPlatformContent content,
        Tagger tagger,
        IApplicationTerms terms,
        UserInformationCache userCache,
        ManagedFileLogic managedFileLogic,
        BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic scheduler,
        AppEventSink sink,
        IEnumerable<IEntityInstanceLogic> entityCreators,
        IRepository<DashboardCandidate> dashItems,
        EntityReferenceLoader loader)
    {
        _alerts = alerts;
        _logger = logger;
        _dataEnvironment = dataEnvironment;
        _workItems = workItems;
        _content = content;
        _tagger = tagger;
        _terms = terms;
        _eventSink = sink;
        _commentsLogic = commentsLogic;
        _comments = comments;
        _userCache = userCache;
        _managedFiles = files;
        _managedFileLogic = managedFileLogic;
        _scheduler = scheduler;
        _tnCache = tnCache;
        _entityCreators = entityCreators;
        _dashItems = dashItems;
        _loader = loader;
    }

    #endregion

    #region creation
    private IEntityInstanceLogic? GetContentCreator(string entityTemplateName)
    {
        return _entityCreators.FirstOrDefault(it=>it.CanCreateWorkItemInstance(entityTemplateName, _content));
    }


    public async Task<(WorkItem, WorkItemTemplate)> HelpCreateWorkItem
        (
            AppEventOrigin? origin,
            string? action,
            string templateName,
            string title,
            string? description,
            bool? isListed, bool? isVisible,
            Guid? userAssignee,
            int? triageAssignee,
            int? status,
            int? priority,
            JObject? creationData,
            IEnumerable<WorkItemLink>? initialLinks,
            WorkItemBookmark? initialBookmark,
            Guid? userId,
            Guid workSet,
            IEnumerable<string>? initialTags,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        var id = Guid.NewGuid();
        var template = _content.GetContentByName<WorkItemTemplate>(templateName)!;
        template.Guarantees().IsNotNull();

        var statusTemplates = template.StatusTemplates;
        var defStatus = statusTemplates.EmptyIfNull().FirstOrDefault(it => it.IsDefault);
        if (defStatus is null && statusTemplates.Any())
            defStatus = statusTemplates.First();
        var givenStatus = defStatus;
        if (status is not null)
            givenStatus = statusTemplates.First(it => it.Id == status.Value);

        var priorityTemplates = template.PriorityTemplates;
        var defPriority = priorityTemplates.EmptyIfNull().FirstOrDefault(it => it.IsDefault);
        if (defPriority is null && priorityTemplates.EmptyIfNull().Any())
            defPriority = priorityTemplates.First();
        var givenPriority = defPriority;
        if(priority is not null)
            givenPriority = priorityTemplates.First(it => it.Id == priority.Value);

        bool givenListed = true;
        if(isListed.HasValue)
            givenListed = isListed.Value;
        bool givenVisible = template.IsVisibleToUsers;
        if (isVisible.HasValue)
            givenVisible = isVisible.Value;

        var eventHistory = new List<WorkItemEventHistory>();
        var currentWorkItemEvent = new WorkItemEventHistory
        {
            EventTime = DateTime.Now,
            Modifier = userId ?? Constants.BuiltIn.SystemUser,
            Priority = givenPriority!.Id,
            Status = givenStatus!.Id,
            TriageAssignee = triageAssignee,
            UserAssignee = userAssignee
        };
        eventHistory.Add(currentWorkItemEvent);

        List<Section> sections = new();
        foreach(var st in template.SectionTemplates)
        {
            if(st.IsCreateOnNew && st.EntityTemplateName is not null)
            {
                var factory = GetContentCreator(st.EntityTemplateName)!;
                if (factory is not null)
                {
                    var sectionCreationData = creationData ?? st.CreationData;
                    var uri = await factory.CreateWorkItemContent(_content, workSet, id, st.EntityTemplateName, st.NewInstanceProcess, sectionCreationData);
                    var section = new Section
                    {
                        Entities = new List<Uri>() { uri },
                        TemplateId = st.Id
                    };

                    sections.Add(section);
                }
            }
        }

        var readyTags = TagUtil.MakeTags(initialTags.EmptyIfNull());
        // todo: count the tags

        var instance = new WorkItem
        {
            Id = id,
            Title = title,
            Description = description,
            IsListed = givenListed,
            IsVisible = givenVisible,
            Status = givenStatus!.Id,
            Priority = givenPriority!.Id,
            Template = templateName,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Creator = userId ?? Constants.BuiltIn.SystemUser,
            LastModifier = userId ?? Constants.BuiltIn.SystemUser,
            HostWorkSet = workSet,
            HostWorkItem = id,
            Tags = readyTags.ToList(),
            UserAssignee = userAssignee,
            TriageAssignee = triageAssignee,
            StartUnresolved = DateTime.UtcNow,
            EventHistory = eventHistory,
            Sections = sections
        };

        if (initialLinks is not null && initialLinks.Any())
        {
            instance.Links.AddRange(initialLinks);
        }

        if (initialBookmark is not null)
        {
            instance.Bookmarks.Add(initialBookmark);
        }

        await _workItems.CreateAsync(trx!, instance);


        _eventSink.BeginBatch(trx);
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(workSet);
        wsTemplate.Guarantees().IsNotNull();

        var topic = $"{wsTemplate}.{templateName}.{context}.workitem_create_instance";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (instance, template);
    }

    public async Task<WorkItemViewModel> ActionCreateWorkItem(
            string? action,
            string templateName,
            string title,
            string? description,
            bool? isListed, bool? isVisible,
            Guid? userAssignee,
            int? triageAssignee,
            int? status,
            int? priority,
            IEnumerable<WorkItemLink>? initialLinks,
            WorkItemBookmark? initialBookmark,
            Guid? userId,
            Guid workSet,
            IEnumerable<string>? initialTags,
            string tzid,
            ITransactionContext? trx,
            bool seal)
    {
        using(PerfTrack.Stopwatch(nameof(ActionCreateWorkItem)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (instance, template) = await HelpCreateWorkItem(
                    null, action, templateName, title, description, isListed, isVisible,
                    userAssignee, triageAssignee, status, priority,
                    null, initialLinks, initialBookmark, userId,
                    workSet, initialTags, trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkItemViewModel.Create(template, instance, _loader, _commentsLogic,
                    _managedFileLogic, _userCache, tzid);

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

    public async Task<(WorkItem,WorkItemTemplate)> EventCreateWorkItem
        (
            AppEventOrigin? origin,
            string? action,
            string templateName,
            string title,
            string? description,
            bool? isListed, bool? isVisible,
            Guid? userAssignee,
            int? triageAssignee,
            int? status,
            int? priority,
            JObject? creationData,
            IEnumerable<WorkItemLink>? initialLinks,
            WorkItemBookmark? initialBookmark,
            Guid? userId,
            Guid workSet,
            IEnumerable<string>? initialTags,
            ITransactionContext? trx,
            bool seal,
            IEnumerable<string>? eventTags = null
        )
    {
        using(PerfTrack.Stopwatch(nameof(EventCreateWorkItem)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var retval = await HelpCreateWorkItem(
                    origin,
                    action,
                    templateName,
                    title, 
                    description,
                    isListed,
                    isVisible,
                    userAssignee,
                    triageAssignee,
                    status,
                    priority,
                    creationData, 
                    initialLinks, 
                    initialBookmark,
                    userId,
                    workSet,
                    initialTags,
                    trx!,
                    seal,
                    "event",
                    eventTags
                    );

                if (isSelfContained)
                    await trx!.CommitAsync();

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


    #endregion

    #region metadata update, assignment, status, priority, title, description, etc

       

    public async Task<(WorkItem,WorkItemTemplate)> HelpEditWorkItem(
            AppEventOrigin? origin,
            string? action,
            Guid id,
            string? title,
            string? description,
            bool? isListed, bool? isVisible,
            Guid? userAssignee,
            int? triageAssignee,
            int? status,
            int? priority,
            Guid? userId,
            IEnumerable<string>? setTags,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        var (instance, rc) = await _workItems.LoadAsync(id);
        instance.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkItemTemplate>(instance.Template)!;
        template.Guarantees().IsNotNull();

        

        var readyTags = TagUtil.MakeTags(setTags.EmptyIfNull());

        if (title is not null) instance.Title = title;
        if (description is not null) instance.Description = description;
        if (status is not null) instance.Status = status.Value;
        if (priority is not null) instance.Priority = priority.Value;
        if (isListed is not null) instance.IsListed = isListed.Value;
        if (isVisible is not null) instance.IsVisible = isVisible.Value;
        if (userAssignee is not null) instance.UserAssignee = userAssignee.Value;
        if (triageAssignee is not null) instance.TriageAssignee = triageAssignee.Value;
        if (setTags is not null && setTags.Any())
            await _tagger.ReconcileTags(trx, instance, readyTags);

        instance.LastModifier = userId ?? Constants.BuiltIn.SystemUser;
        instance.UpdatedDate = DateTime.UtcNow;

        var currentWorkItemEvent = new WorkItemEventHistory
        {
            EventTime = DateTime.UtcNow,
            Modifier = userId ?? Constants.BuiltIn.SystemUser,
            Priority = instance.Priority,
            Status = instance.Status,
            TriageAssignee = triageAssignee,
            UserAssignee = userAssignee
        };

        instance.EventHistory.Add(currentWorkItemEvent);

        await _workItems.UpdateAsync(trx, instance);

        _eventSink.BeginBatch(trx);
        instance.HostWorkSet.Guarantees().IsNotNull();
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(instance.HostWorkSet!.Value);
        wsTemplate.Guarantees().IsNotNull();

        var topic = $"{wsTemplate}.{instance.Template}.{context}.workitem_update_metadata";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (instance, template);
    }


    public async Task<WorkItemViewModel> ActionEditWorkItem(
            string? action,
            Guid id,
            string? title,
            string? description,
            bool? isListed, bool? isVisible,
            Guid? userAssignee,
            int? triageAssignee,
            int? status,
            int? priority,
            Guid? userId,
            IEnumerable<string>? setTags,
            ITransactionContext? trx,
            bool seal,
            string tzid
        )
    {
        using (PerfTrack.Stopwatch(nameof(ActionEditWorkItem)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (instance, template) = await HelpEditWorkItem(null, action,
                    id, title, description, isListed, isVisible, userAssignee, triageAssignee,
                    status, priority, userId, setTags, trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkItemViewModel.Create(template, instance, _loader, _commentsLogic,
                    _managedFileLogic, _userCache, tzid);

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


    public async Task<(WorkItem, WorkItemTemplate)> EventEditWorkItem(
            AppEventOrigin? origin,
            Guid id,
            string? title,
            string? description,
            bool? isListed, bool? isVisible,
            Guid? userAssignee,
            int? triageAssignee,
            int? status,
            int? priority,
            Guid? userId,
            IEnumerable<string>? setTags,
            ITransactionContext trx,
            bool seal,
            IEnumerable<string>? eventTags = null
        )
    {
        using(PerfTrack.Stopwatch(nameof(EventEditWorkItem)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var retval = await HelpEditWorkItem(
                    origin, null, id, title, description, isListed, isVisible, userAssignee, triageAssignee,
                    status, priority, userId, setTags, trx!, seal, "event", eventTags);


                if (isSelfContained)
                    await trx!.CommitAsync();

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
    #endregion

    #region links, bookmarks 

    public async Task<(WorkItem, WorkItemTemplate)> HelpAddWorkItemLink(
            AppEventOrigin? origin,
            string? action,
            WorkItemLink link,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        var (instance, rc) = await _workItems.LoadAsync(id);
        instance.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkItemTemplate>(instance.Template)!;
        template.Guarantees().IsNotNull();

        link.Id = Guid.NewGuid();
        instance.Links.Add(link);

        instance.LastModifier = userId ?? Constants.BuiltIn.SystemUser;
        instance.UpdatedDate = DateTime.UtcNow;

        await _workItems.UpdateAsync(trx, instance);

        _eventSink.BeginBatch(trx);
        instance.HostWorkSet.Guarantees().IsNotNull();
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(instance.HostWorkSet!.Value);
        wsTemplate.Guarantees().IsNotNull();

        var topic = $"{wsTemplate}.{instance.Template}.{context}.workitem_add_link";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (instance, template);

    }

    public async Task<WorkItemViewModel> ActionAddWorkItemLink(
            string? action,
            WorkItemLink link,
            Guid id,
            Guid? userId,
            ITransactionContext? trx,
            bool seal,
            string tzid
        )
    {
        using (PerfTrack.Stopwatch(nameof(ActionAddWorkItemLink)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (instance, template) = await HelpAddWorkItemLink(null, action, link, id,
                     userId, trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkItemViewModel.Create(template, instance, _loader, _commentsLogic,
                    _managedFileLogic, _userCache, tzid);

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

    public async Task<(WorkItem, WorkItemTemplate)> EventAddWorkItemLink(
            AppEventOrigin? origin,
            WorkItemLink link,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            IEnumerable<string>? eventTags = null
        )
    {
        using (PerfTrack.Stopwatch(nameof(EventAddWorkItemLink)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var retval = await HelpAddWorkItemLink(
                    origin, null, link, id, userId, trx!, seal, "event", eventTags);


                if (isSelfContained)
                    await trx!.CommitAsync();

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

    public async Task<(WorkItem, WorkItemTemplate)> HelpRemoveWorkItemLink(
            AppEventOrigin? origin,
            string? action,
            Guid link,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        var (instance, rc) = await _workItems.LoadAsync(id);
        instance.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkItemTemplate>(instance.Template)!;
        template.Guarantees().IsNotNull();

        var item = instance.Links.FirstOrDefault(l => l.Id == link);
        if (item is null)
            return (instance, template);

        instance.Links.Remove(item);

        instance.LastModifier = userId ?? Constants.BuiltIn.SystemUser;
        instance.UpdatedDate = DateTime.UtcNow;

        await _workItems.UpdateAsync(trx, instance);

        _eventSink.BeginBatch(trx);
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(instance.HostWorkSet!.Value);
        wsTemplate.Guarantees().IsNotNull();

        var topic = $"{wsTemplate}.{instance.Template}.{context}.workitem_remove_link";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (instance, template);
    }

    public async Task<WorkItemViewModel> ActionRemoveWorkItemLink(
            string? action,
            Guid link,
            Guid id,
            Guid? userId,
            ITransactionContext? trx,
            bool seal,
            string tzid
        )
    {
        using (PerfTrack.Stopwatch(nameof(ActionRemoveWorkItemLink)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (instance, template) = await HelpRemoveWorkItemLink(null, action, link, id,
                     userId, trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkItemViewModel.Create(template, instance, _loader, _commentsLogic,
                    _managedFileLogic, _userCache, tzid);

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

    public async Task<(WorkItem, WorkItemTemplate)> EventRemoveWorkItemLink(
            AppEventOrigin? origin,
            Guid link,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            IEnumerable<string>? eventTags = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventAddWorkItemLink)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var retval = await HelpRemoveWorkItemLink(
                    origin, null, link, id, userId, trx!, seal, "event", eventTags);


                if (isSelfContained)
                    await trx!.CommitAsync();

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

    public async Task<(WorkItem, WorkItemTemplate)> HelpAddWorkItemBookmark(
            AppEventOrigin? origin,
            string? action,
            WorkItemBookmark bookmark,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        var (instance, rc) = await _workItems.LoadAsync(id);
        instance.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkItemTemplate>(instance.Template)!;
        template.Guarantees().IsNotNull();

        if (instance.Bookmarks.Any(it => it.ApplicationUser == bookmark.ApplicationUser))
            return (instance, template);
        
        instance.Bookmarks.Add(bookmark);

        instance.LastModifier = userId ?? Constants.BuiltIn.SystemUser;
        instance.UpdatedDate = DateTime.UtcNow;

        await _workItems.UpdateAsync(trx, instance);

        _eventSink.BeginBatch(trx);
        instance.HostWorkSet.Guarantees().IsNotNull();
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(instance.HostWorkSet!.Value);
        wsTemplate.Guarantees().IsNotNull();

        var topic = $"{wsTemplate}.{instance.Template}.{context}.workitem_add_bookmark";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (instance, template);

    }

    public async Task<WorkItemViewModel> ActionAddWorkItemBookmark(
            string? action,
            WorkItemBookmark bookmark,
            Guid id,
            Guid? userId,
            ITransactionContext? trx,
            bool seal,
            string tzid
        )
    {
        using (PerfTrack.Stopwatch(nameof(ActionAddWorkItemBookmark)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (instance, template) = await HelpAddWorkItemBookmark(null, action, bookmark, id,
                     userId, trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkItemViewModel.Create(template, instance, _loader, _commentsLogic,
                    _managedFileLogic, _userCache, tzid);

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

    public async Task<(WorkItem, WorkItemTemplate)> EventAddWorkItemBookmark(
            AppEventOrigin? origin,
            WorkItemBookmark bookmark,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            IEnumerable<string>? eventTags = null
        )
    {
        using (PerfTrack.Stopwatch(nameof(EventAddWorkItemBookmark)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var retval = await HelpAddWorkItemBookmark(
                    origin, null, bookmark, id, userId, trx!, seal, "event", eventTags);


                if (isSelfContained)
                    await trx!.CommitAsync();

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

    public async Task<(WorkItem, WorkItemTemplate)> HelpRemoveWorkItemBookmark(
            AppEventOrigin? origin,
            string? action,
            Guid bookmarkUser,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        var (instance, rc) = await _workItems.LoadAsync(id);
        instance.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkItemTemplate>(instance.Template)!;
        template.Guarantees().IsNotNull();

        var item = instance.Bookmarks.FirstOrDefault(bm => bm.ApplicationUser == bookmarkUser);
        if (item is null)
            return (instance, template);

        instance.Bookmarks.Remove(item);

        instance.LastModifier = userId ?? Constants.BuiltIn.SystemUser;
        instance.UpdatedDate = DateTime.UtcNow;

        await _workItems.UpdateAsync(trx, instance);

        _eventSink.BeginBatch(trx);
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(instance.HostWorkSet!.Value);
        wsTemplate.Guarantees().IsNotNull();

        var topic = $"{wsTemplate}.{instance.Template}.{context}.workitem_remove_bookmark";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (instance, template);
    }

    public async Task<WorkItemViewModel> ActionRemoveWorkItemBookmark(
            string? action,
            Guid bookmarkUser,
            Guid id,
            Guid? userId,
            ITransactionContext? trx,
            bool seal,
            string tzid
        )
    {
        using (PerfTrack.Stopwatch(nameof(ActionRemoveWorkItemBookmark)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (instance, template) = await HelpRemoveWorkItemBookmark(null, action, bookmarkUser, id,
                     userId, trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkItemViewModel.Create(template, instance, _loader, _commentsLogic,
                    _managedFileLogic, _userCache, tzid);

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

    public async Task<(WorkItem, WorkItemTemplate)> EventRemoveWorkItemBookmark(
            AppEventOrigin? origin,
            Guid bookmarkUser,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        using (PerfTrack.Stopwatch(nameof(EventRemoveWorkItemBookmark)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var retval = await HelpRemoveWorkItemBookmark(
                    origin, null, bookmarkUser, id, userId, trx!, seal, "event", eventTags);


                if (isSelfContained)
                    await trx!.CommitAsync();

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


    #endregion

    #region section content

    // add section entity (and maybe section)

    public async Task<(WorkItem, WorkItemTemplate)> HelpAddSectionEntity
        (
            AppEventOrigin? origin,
            string? action,
            Guid id,
            int sectionId,
            JObject? creationData,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        var (instance, rc) = await _workItems.LoadAsync(id);
        instance.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkItemTemplate>(instance.Template)!;
        template.Guarantees().IsNotNull();

        var sectionTemplate = template.SectionTemplates.First(it=>it.Id == sectionId);

        bool allowed = context == "event" || sectionTemplate.IsCreateOnDemand;
        allowed.Guarantees().IsTrue();

        var matchingSection = instance.Sections.FirstOrDefault(it=>it.TemplateId == sectionTemplate.Id);
        if(matchingSection is null)
        {
            matchingSection = new Section
            {
                Entities = new(),
                TemplateId = sectionTemplate.Id
            };

            instance.Sections.Add(matchingSection);
        }

        var factory = GetContentCreator(sectionTemplate.EntityTemplateName!)!;
        factory.Guarantees().IsNotNull();

        var sectionCreationData = creationData ?? sectionTemplate.CreationData;
        var uri = await factory.CreateWorkItemContent(
            _content,
            instance.HostWorkSet!.Value, instance.Id, 
            sectionTemplate.EntityTemplateName!, 
            sectionTemplate.NewInstanceProcess, 
            sectionCreationData);

        if (sectionTemplate.IsEntityList || !matchingSection.Entities.Any())
        {
            matchingSection.Entities.Add(uri);
        } else
        {
            matchingSection.Entities[0] = uri;
        }

        await _workItems.UpdateAsync(trx, instance);

        _eventSink.BeginBatch(trx);
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(instance.HostWorkSet!.Value);
        wsTemplate.Guarantees().IsNotNull();

        var topic = $"{wsTemplate}.{instance.Template}.{context}.workitem_add_section";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (instance, template);

    }

    public async Task<WorkItemViewModel> ActionAddSection(
            string action,
            Guid id,
            int sectionId,
            JObject? creationData,
            Guid? userId,
            string tzid,
            ITransactionContext? trx,
            bool seal
            )
    {
        using (PerfTrack.Stopwatch(nameof(ActionAddSection)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (instance, template) = await HelpAddSectionEntity(null, action, id, sectionId, 
                    creationData, userId, trx!, seal, "action", null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkItemViewModel.Create(template, instance, _loader, _commentsLogic,
                    _managedFileLogic, _userCache, tzid);

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

    public async Task<(WorkItem, WorkItemTemplate)> EventAddSection(
            AppEventOrigin? origin,
            Guid id,
            int sectionId,
            JObject? creationData,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            IEnumerable<string>? eventTags = null
        )
    {
        using (PerfTrack.Stopwatch(nameof(EventAddSection)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var retval = await HelpAddSectionEntity(origin, null, id,
                    sectionId, creationData, userId,
                    trx!, seal, "event", eventTags);


                if (isSelfContained)
                    await trx!.CommitAsync();

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


    // remove section entity (and maybe section)
    public async Task<(WorkItem, WorkItemTemplate)> HelpRemoveSectionEntity
        (
            AppEventOrigin? origin,
            string? action,
            Guid id,
            int sectionId,
            Uri entity,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null
        )
    {
        var (instance, rc) = await _workItems.LoadAsync(id);
        instance.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkItemTemplate>(instance.Template)!;
        template.Guarantees().IsNotNull();

        var sectionTemplate = template.SectionTemplates.First(it => it.Id == sectionId);

        bool allowed = context == "event" || sectionTemplate.IsCreateOnDemand;
        allowed.Guarantees().IsTrue();

        var section = instance.Sections.FirstOrDefault(it => it.TemplateId == sectionId);
        if (section is null)
            return (instance, template);

        var factory = GetContentCreator(sectionTemplate.EntityTemplateName!)!;
        factory.Guarantees().IsNotNull();

        if (section.Entities.Contains(entity))
        {
            section.Entities.Remove(entity);
            await factory.DetachRemoveWorkItemEntities(origin, id, entity, trx);
        }

        if(!section.Entities.Any())
            instance.Sections.Remove(section);

        instance.LastModifier = userId ?? Constants.BuiltIn.SystemUser;
        instance.UpdatedDate = DateTime.UtcNow;

        await _workItems.UpdateAsync(trx, instance);

        _eventSink.BeginBatch(trx);
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(instance.HostWorkSet!.Value);
        wsTemplate.Guarantees().IsNotNull();

        var topic = $"{wsTemplate}.{instance.Template}.{context}.workitem_remove_section";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

        return (instance, template);


    }

    public async Task<WorkItemViewModel> ActionRemoveSectionEntity
        (
            string? action,
            Guid id,
            int sectionId,
            Uri entity,
            Guid? userId,
            ITransactionContext? trx,
            bool seal,
            string tzid
        )
    {
        using (PerfTrack.Stopwatch(nameof(ActionRemoveSectionEntity)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (instance, template) = await HelpRemoveSectionEntity(
                    null,
                    action, id, sectionId, entity, userId, trx!, seal, "action",
                    null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                var retval = await WorkItemViewModel.Create(template, instance, _loader, _commentsLogic,
                    _managedFileLogic, _userCache, tzid);

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

    public async Task<(WorkItem, WorkItemTemplate)> EventRemoveSectionEntity
        (
            AppEventOrigin? origin,
            Guid id,
            int sectionId,
            Uri entity,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            IEnumerable<string>? eventTags = null
        )
    {
            using (PerfTrack.Stopwatch(nameof(EventRemoveSectionEntity)))
            {
                bool isSelfContained = trx is null;
                if (isSelfContained)
                    trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

                try
                {
                    var retval = await HelpRemoveSectionEntity(origin, null, id, sectionId,
                        entity, userId, trx!, seal, "event", eventTags);


                    if (isSelfContained)
                        await trx!.CommitAsync();

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


    #endregion

    #region dashboard


    public async Task EnrollIntoDashboardByTags(
        Guid workSet,
        InitialViewData initialData)
    {
        var findTags = initialData.WorkItemAnyTags;
        var (taggedWorkItems, _) = await _workItems
            .GetAllAsync(wi => findTags.Any(ft => wi.Tags.Contains(ft)));

        int total = 0;
        
        foreach (var taggedTag in taggedWorkItems.EmptyIfNull())
            foreach(var em in _entityCreators)
            {
                var instances = await em.InstancesWithAnyTags(taggedTag.Id, initialData.AttachedEntityAnyTags);
                foreach(var instance in instances)
                {
                    var score = instance.EntityTags.Where(et => findTags.Contains(et)).Count();
                    var dc = new DashboardCandidate
                    {
                        Id = Guid.NewGuid(),
                        WorkSet = workSet,
                        Score = score,
                        DescendingOrder = initialData.DescendingOrder,
                        Grouping = initialData.Grouping,
                        Created = DateTime.UtcNow,
                        IsWinner = false,
                        EntityRef = instance.Uri.ToString(),
                        EntityType = instance.EntityType,
                        Tags = instance.EntityTags,
                        MetaTags = new List<string>(),
                        TemplateName = instance.EntityTemplate,
                        Version = 0
                    };

                    await _dashItems.CreateAsync(dc);

                    if (initialData.LimitMatch > 0 && total > initialData.LimitMatch)
                        return;
                }
            }
        
    }

    #endregion

    #region deletion and grooming

    public async Task HelpDeleteWorkItem(
            AppEventOrigin? origin,
            string? action,
            Guid id,
            Guid? userId,
            ITransactionContext trx,
            bool seal,
            string context,
            IEnumerable<string>? eventTags = null

        )
    {
        var (instance, rc) = await _workItems.LoadAsync(id);
        instance.Guarantees().IsNotNull();

        var template = _content.GetContentByName<WorkItemTemplate>(instance.Template)!;
        template.Guarantees().IsNotNull();

        var sectionWork = new List<Task<(WorkItem, WorkItemTemplate)>>();
        foreach(var sectionT in template.SectionTemplates)
        {
            var sections = instance.Sections.Where(it => sectionT.Id == it.TemplateId);
            foreach (var section in sections)
                foreach (var entity in section.Entities)
                    sectionWork.Add(HelpRemoveSectionEntity(origin, action, id, section.TemplateId, entity, userId, trx, seal, context, eventTags));
        }

        await Task.WhenAll(sectionWork);

        await _comments.DeleteFilterAsync(trx, c => c.WorkItem == id);

        var files = await _managedFileLogic.GetAllAttachedFiles(id);
        var work = new List<Task>();
        foreach(var file in files)
            work.Add(_managedFileLogic.DeleteFileAsync(file.Id, userId ?? Constants.BuiltIn.SystemUser));

        await Task.WhenAll(work);

        _eventSink.BeginBatch(trx);
        var wsTemplate = await _tnCache.GetWorkSetTemplateName(instance.HostWorkSet!.Value);
        wsTemplate.Guarantees().IsNotNull();

        _workItems.Delete(instance);

        var topic = $"{wsTemplate}.{instance.Template}.{context}.workitem_delete";
        await _eventSink.Enqueue(origin, topic, action, instance, userId, eventTags, seal);
        await _eventSink.CommitBatch();

    }

    public async Task EventDeleteWorkItem(
        AppEventOrigin? origin,
        Guid id,
        Guid? userId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null
        )
    {
        using (PerfTrack.Stopwatch(nameof(ActionDeleteWorkItem)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteWorkItem(
                    origin,
                    null, id, userId, trx!, seal, "event",
                    eventTags);

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

    public async Task ActionDeleteWorkItem(
        string? action,
        Guid id,
        Guid? userId,
        ITransactionContext? trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null
        )
    {
        using (PerfTrack.Stopwatch(nameof(ActionDeleteWorkItem)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteWorkItem(
                    null,
                    action, id, userId, trx!, seal, context,
                    null);

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

    public async Task DeleteGroomableWorkItems()
    {
        var trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

        try
        {
            var templates = _content.GetAllContent<WorkItemTemplate>()
                .Where(it => it.IsGroomable && it.GroomPeriod is not null);

            int count = 0;

            foreach (var template in templates)
            {
                var period = template.GroomPeriod;
                var backFrom = period!.BackFrom(DateTime.UtcNow);
                var resolvedStatus = template
                    .StatusTemplates
                    .Where(st => st.StatusType == StatusType.Resolved)
                    .Select(it => it.Id)
                    .ToArray();

                RepositoryContext rc;
                List<WorkItem> items = new();
                string groomType = "groomed";

                switch (template.GroomBehavior)
                {
                    case WorkItemGroomBehavior.FromCreated:
                        {
                            (items, rc) = await _workItems.GetAllAsync(trx, wi => wi.Template == template.Name && wi.CreatedDate < backFrom);
                            groomType = "workitem_created_groomed";
                        }
                        break;
                    case WorkItemGroomBehavior.FromModified:
                        {
                            (items, rc) = await _workItems.GetAllAsync(trx, wi => wi.Template == template.Name && wi.UpdatedDate < backFrom);
                            groomType = "workitem_updated_groomed";
                        }
                        break;
                    case WorkItemGroomBehavior.FromResolved:
                        {
                            (items, rc) = await _workItems.GetAllAsync(trx, wi => wi.Template == template.Name && wi.UpdatedDate < backFrom && resolvedStatus.Contains(wi.Status));
                            groomType = "workitem_resolved_groomed";
                        }
                        break;

                }

                if (items is null)
                    return;

                foreach (var item in items)
                {
                    var origin = new AppEventOrigin(nameof(DeleteGroomableWorkItems), null, null);
                    await HelpDeleteWorkItem(origin, null, item.Id, Constants.BuiltIn.SystemUser, trx, false, "event", null);

                    _eventSink.BeginBatch(trx);
                    var wsTemplate = await _tnCache.GetWorkSetTemplateName(item.HostWorkSet!.Value);
                    wsTemplate.Guarantees().IsNotNull();

                    var topic = $"{wsTemplate}.{item.Template}.event.{groomType}";

                    await _eventSink.Enqueue(origin, topic, null, item, Constants.BuiltIn.SystemUser, null, false);

                    count += 1;
                    if (count > 100) // that's enough for now
                        break;
                }

                await _eventSink.CommitBatch();

                await trx.CommitAsync();


            }
        } catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.General,
                   LogLevel.Information, ex.TraceInformation());
            await trx.AbortAsync();
        }
        
    }


    #endregion

    #region retrieval
    
    // raw instance
    public async Task<WorkItem> GetRawWorkItem(Guid id)
    {
        var (item, _) = await _workItems.LoadAsync(id);
        return item;
    }

    // instance vm
    public async Task<WorkItemViewModel > GetWorkItemViewModel(Guid id, string tzid)
    {
        var (item, _) = await _workItems.LoadAsync(id);
        item.Guarantees().IsNotNull();
        var template = _content.GetContentByName<WorkItemTemplate>(item.Template)!;
        template.Guarantees().IsNotNull();
        return await WorkItemViewModel.Create(template, item, _loader, _commentsLogic, _managedFileLogic, _userCache, tzid);
    }

    // instance vms by workset and template, optionally tags
    public async Task<List<WorkItemViewModel>> GetWorkItems(
        int page,
        Guid workSet, 
        string? templateName, 
        IList<string>? tags,
        int? status,
        Guid? userAssignee,
        int? triageAssignee,
        string tzid)
    {
        var predicate = PredicateBuilder.New<WorkItem>();
        predicate = predicate.And(wi => wi.HostWorkSet == workSet);

        if (!string.IsNullOrWhiteSpace(templateName))
            predicate = predicate.And(wi => wi.Template == templateName);

        if(tags is not null && tags.Any())
        {
            var readyTags = TagUtil.MakeTags(tags);
            predicate = predicate.And(wi => readyTags.Any(rt => wi.Tags.Contains(rt)));
        }

        if(status is not null)
        {
            predicate = predicate.And(wi => wi.Status == status.Value);
        }

        if(userAssignee is not null)
        {
            predicate = predicate.And(wi=> wi.UserAssignee == userAssignee.Value);
        } else if (triageAssignee is not null)
        {
            predicate = predicate.And(wi => wi.TriageAssignee == triageAssignee.Value);
        }

        var (items,_) = await _workItems.GetOrderedPageAsync(wi => wi.UpdatedDate, true, page, predicate);

        var templates = new Dictionary<string, WorkItemTemplate>();
        foreach(var wi in items)
        {
            if(!templates.ContainsKey(wi.Template))
            {
                var template = _content.GetContentByName<WorkItemTemplate>(wi.Template)!;
                template.Guarantees().IsNotNull();
                templates.Add(wi.Template, template);
            }
        }

        var work = new List<Task<WorkItemViewModel>>();
        work.AddRange(
                items.Select(wi => WorkItemViewModel.Create(templates[wi.Template], wi, _loader, _commentsLogic, _managedFileLogic, _userCache, tzid)));

        await Task.WhenAll(work);

        return work.Select(it => it.Result).ToList();
    }


    // raw template 
    public Task<WorkItemTemplate?> GetRawWorkItemTemplate(string name)
    {
        return Task.FromResult(_content.GetContentByName<WorkItemTemplate>(name));
    }

    // template vm
    public Task<WorkItemTemplateViewModel> GetWorkItemTemplateViewModel(string name)
    {
        var template = _content.GetContentByName<WorkItemTemplate>(name)!;
        template.Guarantees().IsNotNull();
        return Task.FromResult(WorkItemTemplateViewModel.Create(template));
    }


    // template vms by tag
    public Task<List<WorkItemTemplateViewModel>> GetWorkItemTemplates()
    {
        var templates = _content.GetAllContent<WorkItemTemplate>();
        var retval = templates.Select(t => WorkItemTemplateViewModel.Create(t)).ToList();
        return Task.FromResult(retval);
    }


    #endregion




}
