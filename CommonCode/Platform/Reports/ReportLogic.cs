using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Notification;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Comments;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
using BFormDomain.CommonCode.Platform.Tables;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Reports;

/// <summary>
/// ReportLogic amanges CRUD and tagging operations for ReportInstances.
///     -References:
///         >AcceptReportInstanceContent.cs
///         >ReportEntityLoaderModule.cs
///         >ReportLogic.cs
///         >RuleActionCreateReport.cs
///     -Functions:
///         >HelpCreateReport
///         >ActionCreateReport
///         >EventCreateReport
///         >EventAddReportTags
///         >ActionAddReportTags
///         >EventRemoveReportTags
///         >ActionRemoveReportTags
///         >HelpDeleteReportInstance
///         >EventDeleteReport
///         >ActionDeleteReport
///         >EventDeleteWorkItemReportInstances
///         >EventDeleteWorkItemReportInstance
///         >GetReportInstanceRef
///         >GetReportInstance
///         >GetReportInstanceSummary
///         >GetReports
///         >GetAllReportTemplates
///         >GetReportTemplateVM
///         >GetTaggedReportInstanceRefs
///         >GetTaggedReportInstanceRefs
/// </summary>
public class ReportLogic
{
    #region fields
    private readonly IApplicationAlert _alerts;
    private readonly IRepository<ReportInstance> _reports;
    private readonly ILogger<ReportLogic> _logger;
    private readonly IApplicationPlatformContent _content;
    private readonly Tagger _tagger;
    private readonly IApplicationTerms _terms;
    private readonly AppEventSink _eventSink;
    private readonly IRepository<Comment> _comments;
    private readonly UserInformationCache _userCache;
    private readonly TemplateNamesCache _tnCache;
    private readonly IDataEnvironment _dataEnvironment;
    private readonly TableLogic _tableLogic;
    private readonly BFormOptions _appOptions;
    private readonly IRepository<NotificationGroup> _notificationGroups;
    private readonly BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic _scheduler;
    private readonly RequestNotification _notification;
    private readonly UserInformationCache _userInformationCache;
    private readonly CommentsLogic _commentsLogic;
    #endregion

    #region ctor
    public ReportLogic(
        IApplicationAlert alerts,
        IRepository<ReportInstance> reports,
        ILogger<ReportLogic> logger,
        IApplicationPlatformContent content,
        Tagger tagger,
        IApplicationTerms terms,
        AppEventSink sink,
        IRepository<Comment> comments,
        UserInformationCache userCache,
        TemplateNamesCache tnCache,
        IDataEnvironment dataEnvironment,
        TableLogic tableLogic,
        IOptions<BFormOptions> appOptions,
        IRepository<NotificationGroup> notificationGroups,
        BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic scheduler,
        RequestNotification notification,
        UserInformationCache userInformationCache,
        CommentsLogic commentsLogic)
    {
        _alerts = alerts;
        _reports = reports;
        _logger = logger;
        _content = content;
        _tagger = tagger;
        _terms = terms;
        _eventSink = sink;
        _comments = comments;
        _userCache = userCache;
        _tnCache = tnCache;
        _dataEnvironment = dataEnvironment;
        _tableLogic = tableLogic;
        _appOptions = appOptions.Value;
        _notificationGroups = notificationGroups;
        _scheduler = scheduler;
        _notification = notification;
        _userInformationCache = userInformationCache;
        _commentsLogic = commentsLogic;
    }
    #endregion

    #region creation

    public async Task<(ReportTemplate, ReportInstance)> HelpCreateReport(
        AppEventOrigin? origin,
        string? action,
        string templateName,
        Guid? userId,
        Guid workSet,
        Guid workItem,

        JObject queryFormData,
        IEnumerable<string>? initialTags,
        string? tzId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {
        // TODO: Fancier report creation. 
        // There should be a background service that sees report requests from the message bus,
        // gets the data, renders the report, creates the instance,
        // and records or sends back metadata about the task to anyone interested.
        // The pattern that follows will only really work well if reports
        // don't take a long time to generate. Will work okay for quickly generated
        // reports.

        var template = _content.GetContentByName<ReportTemplate>(templateName)!;
        template.Guarantees().IsNotNull();

        // create a query
        var tq = template.Query.BuildQuery(queryFormData, out Guid? wsSpec, out Guid? wiSpec);

        // fetch the data
        var data = await _tableLogic.QueryDataTableAll(
            template.Query.TableTemplate, tq,
            wsSpec, wiSpec);

        // don't do empty reports
        data.Guarantees($"{templateName} generating empty report with: {queryFormData}").IsNotNull();
        data.Data.Guarantees($"{templateName} generating empty report with: {queryFormData}").IsNotEmpty();

        // no timezone = default
        if (string.IsNullOrWhiteSpace(tzId))
            tzId = _appOptions.GetLocalTimeZoneId();
        var dataSet = data.MakeDataSet(tzId);

        // instantiate the report renderer
        var report = template.BuildReport(dataSet, queryFormData, tzId);

        // render the report
        var html = report.GenerateReport();
        html.Guarantees().IsNotNullOrEmpty();

        // record instance of the report to the repository
        var id = Guid.NewGuid();
        var readyTags = TagUtil.MakeTags(initialTags.EmptyIfNull());

        var reportInstance = new ReportInstance
        {
            Id = id,
            Version = 0,
            Template = template.Name,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Creator = userId ?? Constants.BuiltIn.SystemUser,
            LastModifier = userId ?? Constants.BuiltIn.SystemUser,
            HostWorkSet = workSet,
            HostWorkItem = workItem,
            Html = html,
            Title = report.ReportTitle,
            Tags = readyTags.ToList()
        };

        await _reports.CreateAsync(trx!, reportInstance);

        // count and track tags
        foreach (var tag in readyTags)
            await _tagger.CountTags(reportInstance, trx, 1, tag);

        // set grooming date
        if (template.GroomingLifeDays != 0)
            reportInstance.GroomDate = DateTime.UtcNow.AddDays(template.GroomingLifeDays);


        // send out events
        _eventSink.BeginBatch(trx!);
        var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(workSet, workItem);
        var topic = $"{workSetTemplateName}.{workItemTemplateName}.{templateName}.{context}.report_create_instance";
        await _eventSink.Enqueue(origin, topic, action, reportInstance, userId, eventTags, seal);
        await _eventSink.CommitBatch();


        // possibly send notifications.
        if (template.NotificationGroupsWithTags.Any())
        {
            var (sendTo, _) = await _notificationGroups.GetAllAsync(ng => ng.Active && template.NotificationGroupsWithTags.Any(it => ng.Tags.Contains(it)));
            if (sendTo is not null && sendTo.Any()
                && (template.CallNotify || template.SMSNotify || template.ToastNotify || template.EmailNotify))
            {
                var simpleMessage = $"Report {report.ReportTitle} is available.";
                var baseUrl = new Uri(_appOptions.DomainBaseUrl);
                var reportStr = _appOptions.ReportViewerUrl;
                reportStr = reportStr.Replace("{id}", id.ToString());
                var reportUrl = new Uri(reportStr);
                var getReportUrl = new Uri(baseUrl, reportUrl);
                var fancyMessage = $"Report {report.ReportTitle} is available.{Environment.NewLine}<br/><a href = \"{getReportUrl}\">See the report.</a>";

                var nm = new NotificationMessage
                {
                    CreatorId = Constants.BuiltIn.SystemUser.ToString(),
                    Severity = LogLevel.Information,
                    Subject = template.TitleTemplate,
                };

                if (template.EmailNotify)
                    nm.EmailHtmlText = fancyMessage;
                if (template.SMSNotify)
                    nm.SMSText = simpleMessage;
                if (template.ToastNotify)
                    nm.ToastText = simpleMessage;
                if (template.CallNotify)
                    nm.CallText = simpleMessage;

                if (template.SuppressionMinutes.HasValue && template.SuppressionMinutes > 0)
                {
                    nm.WantSuppression = true;
                    nm.SuppressionMinutes = template.SuppressionMinutes.Value;
                }

                nm.NotificationGroups.AddRange(sendTo.Select(it => it.Id));

                await _notification.Request(nm);



            }
        }


        return (template, reportInstance);
    }



    public async Task<Guid> ActionCreateReport
        (string action,
        string reportTemplate,
        Guid workSet,
        Guid workItem,
        Guid userId,
        JObject queryFormData,
        IEnumerable<string>? initialTags,
        string tzId,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionCreateReport)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (template, instance) = await HelpCreateReport(
                    null,
                    action,
                    reportTemplate,
                    userId,
                    workSet, workItem,
                    queryFormData,
                    initialTags,
                    tzId,
                    trx!,
                    false,
                    "action",
                    null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                return instance.Id;

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

    public async Task<Guid> EventCreateReport(
        AppEventOrigin origin,
        string reportTemplate,
        Guid workSet,
        Guid workItem,
        JObject queryFormData,
        IEnumerable<string>? initialTags,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using(PerfTrack.Stopwatch(nameof(EventCreateReport)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);


            try
            {
                var (template, instance) = await HelpCreateReport(
                    origin, null, reportTemplate,
                    null,
                    workSet, workItem,
                    queryFormData,
                    initialTags,
                    null,
                    trx!,
                    seal,
                    "event",
                    null);

                if (isSelfContained)
                    await trx!.CommitAsync();

                return instance.Id;

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
    #endregion

    #region tagging
    public async Task EventAddReportTags(
        AppEventOrigin origin,
        Guid instanceId,
        IEnumerable<string> tags,
        bool seal,
        IEnumerable<string>? eventTags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventAddReportTags)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _reports.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();


                // set data
                instance.LastModifier = origin.Preceding?.OriginUser;
                instance.UpdatedDate = DateTime.UtcNow;

                // tagscommands
                if (tags is not null && tags.Any())
                {
                    await _tagger.Tag(trx!, instance, _reports, tags);

                    // events
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.report_update_tags";
                    await _eventSink.Enqueue(origin, topic, null, instance, null, eventTags, seal);
                    await _eventSink.CommitBatch();
                }

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

    public async Task ActionAddReportTags(
        string action,
        Guid instanceId,
        Guid userId,
        bool seal,
        IEnumerable<string>? tags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionAddReportTags)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _reports.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();


                // set data
                instance.LastModifier = userId;
                instance.UpdatedDate = DateTime.UtcNow;

                // tags
                if (tags is not null && tags.Any())
                {
                    await _tagger.Tag(trx!, instance, _reports, tags);

                    // events
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.report_update_tags";
                    await _eventSink.Enqueue(null, topic, action, instance, userId, null, seal);
                    await _eventSink.CommitBatch();
                }

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

    public async Task EventRemoveReportTags(
        AppEventOrigin origin,
        Guid instanceId,
        IEnumerable<string> tags,
        bool seal,
        IEnumerable<string>? eventTags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventRemoveReportTags)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _reports.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();


                // set data
                instance.LastModifier = origin.Preceding?.OriginUser;
                instance.UpdatedDate = DateTime.UtcNow;

                // tagscommands
                if (tags is not null && tags.Any())
                {
                    await _tagger.Untag(trx!, instance, _reports, tags);

                    // events
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.report_update_tags";
                    await _eventSink.Enqueue(origin, topic, null, instance, null, eventTags, seal);
                    await _eventSink.CommitBatch();
                }

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

    public async Task ActionRemoveReportTags(
        string action,
        Guid instanceId,
        Guid userId,
        bool seal,
        IEnumerable<string>? tags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionRemoveReportTags)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _reports.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();


                // set data
                instance.LastModifier = userId;
                instance.UpdatedDate = DateTime.UtcNow;

                // tags
                if (tags is not null && tags.Any())
                {
                    await _tagger.Untag(trx!, instance, _reports, tags);

                    // events
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.report_update_tags";
                    await _eventSink.Enqueue(null, topic, action, instance, userId, null, seal);
                    await _eventSink.CommitBatch();
                }

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

    #region deletions

    private async Task HelpDeleteReportInstance(
        Guid id,
        AppEventOrigin? origin,
        string? action,
        Guid? userId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {
        using (PerfTrack.Stopwatch(nameof(HelpDeleteReportInstance)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (existing, _) = await _reports.LoadAsync(id);
                if (existing is null)
                    return;

                await _reports.DeleteAsync(existing);

                await _comments.DeleteFilterAsync(trx!, c => c.HostEntity == id);

                await _tagger.Untag(trx!, existing, _reports, existing.Tags);
                                
                
                _eventSink.BeginBatch(trx!);
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(existing.HostWorkSet!.Value, existing.HostWorkItem!.Value!);
                var topic = $"{workSetTemplateName}.{workItemTemplateName}.{existing.Template}.{context}.report_delete_instance";
                await _eventSink.Enqueue(origin, topic, action, existing, userId, eventTags, seal);
                await _eventSink.CommitBatch();

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

    public async Task EventDeleteReport(
        Guid id,
        AppEventOrigin origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteReport)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteReportInstance(id, origin, null, null, trx!, seal, "event", null);

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

    public async Task ActionDeleteReport(
       Guid id,
       string action,
       Guid userId,
       bool seal = false,
       ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionDeleteReport)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteReportInstance(id, null, action, userId, trx!, seal, "action", null);

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


    public async Task EventDeleteWorkItemReportInstances(
        Guid workItem,
        AppEventOrigin origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteWorkItemReportInstances)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (matching, _) = await _reports.GetAllAsync(fi => fi.HostWorkItem == workItem);
                if (!matching.Any())
                    return;

                var work = new List<Task>();
                foreach (var item in matching)
                {
                    work.Add(HelpDeleteReportInstance(item.Id, origin, null, null, trx!, seal, "event", null));
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

    public async Task EventDeleteWorkItemReportInstance(
        Guid workItem,
        Uri entityUri,
        AppEventOrigin? origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteWorkItemReportInstances)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            bool wantsTemplate = entityUri.Segments.Any(it => it.ToLowerInvariant() == "template");
            if (wantsTemplate)
                return;

            

            try
            {
                var id = new Guid(entityUri.Segments.Last());

                var (matching, _) = await _reports.LoadAsync(id);
                if (matching is null || matching.HostWorkItem != workItem)
                    return;

                await HelpDeleteReportInstance(id, origin, null, null, trx!, seal, "event", null);
                
                

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

    #region retrievals

    public async Task<Uri> GetReportInstanceRef(Guid id)
    {
        var (item, _) = await _reports.LoadAsync(id);
        item.Guarantees().IsNotNull();
        return item.MakeReference();
    }



    public async Task<ReportInstanceViewModel> GetReportInstance(Guid id, string tzId)
    {
        using(PerfTrack.Stopwatch(nameof(GetReportInstance)))
        {
            var (item, _) = await _reports.LoadAsync(id);
            item.Guarantees().IsNotNull();

            var template = _content.GetContentByName<ReportTemplate>(item.Template)!;
            template.Guarantees().IsNotNull();

            var retval = await ReportInstanceViewModel.CreateView(
                item, template, tzId, _userCache, _commentsLogic);

            return retval;

        }
    }

    public async Task<ReportInstanceSummaryViewModel> GetReportInstanceSummary(Guid id, string tzId)
    {
        using (PerfTrack.Stopwatch(nameof(GetReportInstance)))
        {
            var (item, _) = await _reports.LoadAsync(id);
            item.Guarantees().IsNotNull();

            var template = _content.GetContentByName<ReportTemplate>(item.Template)!;
            template.Guarantees().IsNotNull();

            var retval = await ReportInstanceSummaryViewModel.CreateSummary(
                        item, template, tzId, _userCache);

            return retval;

        }
    }

    public async Task<IList<ReportInstanceSummaryViewModel>> GetReports(Guid workItem, string tzId, int page)
    {
        using(PerfTrack.Stopwatch(nameof(GetReports)))
        {
            var (data, _) = await _reports.GetOrderedPageAsync<DateTime>(
                ri => ri.CreatedDate, descending: true, page: page,
                predicate: ri => ri.HostWorkItem == workItem);
            
            var retval = new List<ReportInstanceSummaryViewModel>();
            foreach (var item in data)
            {
                var template = _content.GetContentByName<ReportTemplate>(item.Template)!;
                if (template is not null)
                {

                    var vm = await ReportInstanceSummaryViewModel.CreateSummary(
                        item, template, tzId, _userCache);

                    retval.Add(vm);
                }
            }

            return retval;


        }
    }


    public Task<IList<ReportTemplateViewModel>> GetAllReportTemplates()
    {
        using(PerfTrack.Stopwatch(nameof(GetAllReportTemplates)))
        {
            var templates = _content.GetAllContent<ReportTemplate>();
            return Task.FromResult(templates.Select(it => ReportTemplateViewModel.Create(it)).ToIList());
        }
    }

    public ReportTemplateViewModel? GetReportTemplateVM(string templateName)
    {
        var template = _content.GetContentByName<ReportTemplate>(templateName)!;
        template.Guarantees().IsNotNull();
        return ReportTemplateViewModel.Create(template);
    }


    public async Task<List<EntitySummary>> GetTaggedReportInstanceRefs(Guid workItem, IEnumerable<string> tags)
    {
        using (PerfTrack.Stopwatch(nameof(GetTaggedReportInstanceRefs)))
        {
            var readyTags = TagUtil.MakeTags(tags);
            var (instances, _) = await _reports.GetAllAsync(fi => readyTags.Any(rt => fi.HostWorkItem == workItem && fi.Tags.Contains(rt)));

            var retval = instances.Select(it => new EntitySummary
            {
                Uri = it.MakeReference(),
                EntityType = it.EntityType,
                EntityTags = it.Tags,
                EntityTemplate = it.Template
            }).ToList();
            return retval;

        }
    }
    #endregion

}
