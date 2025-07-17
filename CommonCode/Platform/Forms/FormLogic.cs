using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Comments;
using BFormDomain.CommonCode.Platform.Content;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.ManagedFiles;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Forms;

/// <summary>
/// FormLogic manages CRUD operations for creating forms
/// </summary>
public class FormLogic
{

    #region fields
    private readonly IApplicationAlert _alerts;
    private readonly ILogger<FormLogic> _logger;
    private readonly IDataEnvironment _dataEnvironment;
    private readonly IRepository<FormInstance> _instances;
    private readonly Tagger _tagger;
    private readonly IApplicationTerms _terms;
    private readonly AppEventSink _eventSink;
    private readonly IRepository<Comment> _comments;
    private readonly UserInformationCache _userCache;
    private readonly TemplateNamesCache _tnCache;
    private readonly IRepository<ManagedFileInstance> _managedFiles;
    private readonly ManagedFileLogic _managedFileLogic;
    private readonly BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic _scheduler;
    private readonly FormHelperLogic _formHelperLogic;
    private readonly IApplicationPlatformContent _content;
    private ConcurrentDictionary<string,FormTemplateViewModel> _formTemplateVMs = new();
    #endregion

    #region ctor
    public FormLogic(
        IApplicationAlert alerts,
        ILogger<FormLogic> logger,
        IDataEnvironment dataEnvironment,
        IRepository<FormInstance> formInstances,
        TemplateNamesCache tnCache,
        IRepository<Comment> comments,
        IRepository<ManagedFileInstance> files,
        Tagger tagger,
        IApplicationTerms terms,
        UserInformationCache userCache,
        ManagedFileLogic managedFileLogic,
        BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic scheduler,
        AppEventSink sink,
        FormHelperLogic formhelper,
        IApplicationPlatformContent content)
    {
        _alerts = alerts;
        _logger = logger;
        _dataEnvironment = dataEnvironment;
        _instances = formInstances;
        _tagger = tagger;
        _terms = terms;
        _eventSink = sink;
        _comments = comments;
        _userCache = userCache;
        _managedFiles = files;
        _managedFileLogic = managedFileLogic;
        _scheduler = scheduler;
        _tnCache = tnCache;
        _formHelperLogic = formhelper;
        _content = content;
    }
    #endregion

    private void ValidateFormContentUsingSchema(string contentJson, FormInstance instance)
    {
        FormHelperLogic.ValidateFormContentUsingSchema(_content, contentJson, instance);

    }

    #region create form instance
    private async Task<(Guid,string?,FormTemplateViewModel)> HelpCreateFormInstance(
        AppEventOrigin? origin,
        string? action,
        string templateName,
        string? initialPropertiesName,
        string? initialPropertiesJson,
        Guid? userId,
        Guid workSet,
        Guid workItem,
        FormInstanceHome home,
        IEnumerable<string>? initialTags,
        ITransactionContext trx,
        bool seal,
        string context, IEnumerable<string>? eventTags = null)
    {

        return await _formHelperLogic.HelpCreateFormInstance(
            _content, origin, action, 
                        templateName, initialPropertiesName, initialPropertiesJson, 
                        userId, workSet, workItem, 
                        home, initialTags, trx, seal, context, eventTags);

       
    }

    public async Task<FormInstanceViewModel> ActionCreateForm(
        string action,
        string templateName,
        Guid workSet, 
        Guid workItem,
        Guid userId,
        FormInstanceHome home,
        IEnumerable<string>? initialTags,
        string? initialPropertiesName = null,
        string? initialPropertiesJson = null,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionCreateForm)))
        {

            bool isSelfContained = trx is null;
            if(isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {

                var (id, initialPropertiesJsonValues, formTemplateVM) = 
                    await HelpCreateFormInstance(
                        null,
                        action, templateName, initialPropertiesName, initialPropertiesJson,
                        userId, workSet,
                        workItem, home, initialTags, trx!,
                        seal, "action", null);

                if(isSelfContained)
                    await trx!.CommitAsync();

                var retval = new FormInstanceViewModel
                {
                    Id = id,
                    ActionCompletionId = action,
                    WorkSet = workSet,
                    WorkItem = workItem,
                    Creator = userId,
                    Created = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    JsonContent = initialPropertiesJsonValues,
                    Home = home,
                    Tags = new(TagUtil.MakeTags(initialTags.EmptyIfNull())),
                    Template = formTemplateVM
                };

                return retval;
            }
            catch (Exception ex)
            {
                _alerts.RaiseAlert(ApplicationAlertKind.General,
                    LogLevel.Information, ex.TraceInformation());
                if(isSelfContained)
                    await trx!.AbortAsync();

                throw;
            }
        }
    }

    public async Task<Guid> EventCreateForm(
        AppEventOrigin origin,
        string templateName,
        Guid workSet,
        Guid workItem,
        FormInstanceHome home,
        IEnumerable<string>? initialTags,
        string? initialPropertiesJson = null,
        string? initialPropertiesName = null,
        bool seal = false, ITransactionContext? trx = null)
    {
        using(PerfTrack.Stopwatch(nameof(EventCreateForm)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {

                var (id, _, __) =
                    await HelpCreateFormInstance(
                        origin,
                        null, templateName,
                        initialPropertiesName, initialPropertiesJson, 
                        null, workSet,
                        workItem, home, initialTags, trx!,
                        seal, "event", null);

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


    public async Task<IList<FormInstanceViewModel>> ActionCreateManyForms(
        IEnumerable<CreateFormInstancesCommand> commands,
        string action,
        Guid workSet,
        Guid workItem,
        Guid userId,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        commands.Requires().IsNotNull();
        if (!commands.Any())
            return Enumerable.Empty<FormInstanceViewModel>().ToList();

        using(PerfTrack.Stopwatch(nameof(ActionCreateManyForms)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var work = new List<Task<FormInstanceViewModel>>();

                foreach(var command in commands)
                {
                    work.Add(ActionCreateForm(action, command.TemplateName, workSet, workItem, userId, command.Home, command.InitialTags, command.InitialPropertiesName, command.InitialPropertiesName, seal, trx));
                    
                }

                await Task.WhenAll(work);

                return work.Select(it => it.Result).ToList();

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


    public async Task EventCreateManyForms(
        IEnumerable<CreateFormInstancesCommand> commands,
        AppEventOrigin origin,
        Guid workSet,
        Guid workItem,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        commands.Requires().IsNotNull();
        if (!commands.Any())
            return;

        using (PerfTrack.Stopwatch(nameof(ActionCreateManyForms)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var work = new List<Task>();

                foreach (var command in commands)
                {
                    work.Add(EventCreateForm(origin, command.TemplateName, workSet, workItem, command.Home, command.InitialTags, null, command.InitialPropertiesName, seal, trx));
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

    #region update form instance
    public async Task ActionUpdateFormContent(
        string action,
        Guid instanceId,
        string contentJson,
        Guid userId,
        bool seal,
        IEnumerable<string>? tags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionUpdateFormContent)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _instances.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();
                ValidateFormContentUsingSchema(contentJson, instance);

                // set data
                instance.Content = BsonDocument.Parse(contentJson);
                instance.LastModifier = userId;
                instance.UpdatedDate = DateTime.UtcNow;

                // tags
                if (tags is not null && tags.Any())
                    await _tagger.ReconcileTags(trx!, instance, tags);
                await _instances.UpdateAsync(trx!, (instance, tc));

                // events
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                _eventSink.BeginBatch(trx!);
                string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.action.form_update_content";
                await _eventSink.Enqueue(null, topic, action, instance, userId, null, seal);
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

    public async Task EventUpdateFormContent(
        AppEventOrigin origin,
        Guid instanceId,
        string contentJson,
        IEnumerable<string>? eventTags,
        bool seal,
        IEnumerable<string>? tags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventUpdateFormContent)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _instances.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();

                ValidateFormContentUsingSchema(contentJson, instance);

                // set data
                instance.Content = BsonDocument.Parse(contentJson);
                instance.LastModifier = origin.Preceding?.OriginUser;
                instance.UpdatedDate = DateTime.UtcNow;

                // tags
                if (tags is not null && tags.Any())
                    await _tagger.ReconcileTags(trx!, instance, tags);

                await _instances.UpdateAsync(trx!, (instance, tc));

                

                // events
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                _eventSink.BeginBatch(trx!);
                string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.form_update_content";
                await _eventSink.Enqueue(origin, topic, null, instance, null, eventTags, seal);
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

    public async Task ActionUpdateManyFormsContent(
        IEnumerable<UpdateFormInstancesCommand> commands, 
        string action,
        Guid userId,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        commands.Requires().IsNotNull();
        if (!commands.Any())
            return;

        using (PerfTrack.Stopwatch(nameof(ActionUpdateManyFormsContent)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var work = new List<Task>();

                foreach (var command in commands)
                {
                    work.Add(ActionUpdateFormContent(action, command.Id,  command.ContentJson, userId, seal, command.Tags, trx));

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


    public async Task EventUpdateManyFormsContent(
        IEnumerable<UpdateFormInstancesCommand> commands,
        AppEventOrigin origin,
        IEnumerable<string>? eventTags,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        commands.Requires().IsNotNull();
        if (!commands.Any())
            return;

        using (PerfTrack.Stopwatch(nameof(ActionUpdateManyFormsContent)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var work = new List<Task>();

                foreach (var command in commands)
                {
                    work.Add(EventUpdateFormContent(origin, command.Id, command.ContentJson, eventTags!.EmptyIfNull(), seal, command.Tags, trx));

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

    #region custom actions
    public async Task ActionInvokeCustomAction(
        string action,
        int buttonId,
        Guid instanceId,
        Guid userId,
        ITransactionContext? trx = null)
    {
        await _formHelperLogic.ActionInvokeCustomAction(_content, action, buttonId, instanceId, userId, trx);
        

    }
    #endregion  

    #region tagging
    public async Task EventAddFormTags(
        AppEventOrigin origin,
        Guid instanceId,
        IEnumerable<string> tags,
        bool seal,
        IEnumerable<string>? eventTags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventAddFormTags)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _instances.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();

                
                // set data
                instance.LastModifier = origin.Preceding?.OriginUser;
                instance.UpdatedDate = DateTime.UtcNow;

                // tagscommands
                if (tags is not null && tags.Any())
                {
                    await _tagger.Tag(trx!, instance, _instances, tags);

                    // events
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.form_update_tags";
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

    public async Task ActionAddFormTags(
        string action,
        Guid instanceId,
        Guid userId,
        bool seal,
        IEnumerable<string>? tags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionAddFormTags)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _instances.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();
                

                // set data
                instance.LastModifier = userId;
                instance.UpdatedDate = DateTime.UtcNow;

                // tags
                if (tags is not null && tags.Any())
                {
                    await _tagger.Tag(trx!, instance, _instances, tags);

                    // events
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.form_update_tags";
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

    public async Task EventRemoveFormTags(
        AppEventOrigin origin,
        Guid instanceId,
        IEnumerable<string> tags,
        bool seal,
        IEnumerable<string>? eventTags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventRemoveFormTags)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _instances.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();


                // set data
                instance.LastModifier = origin.Preceding?.OriginUser;
                instance.UpdatedDate = DateTime.UtcNow;

                // tagscommands
                if (tags is not null && tags.Any())
                {
                    await _tagger.Untag(trx!, instance, _instances, tags);

                    // events
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.form_update_tags";
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

    public async Task ActionRemoveFormTags(
        string action,
        Guid instanceId,
        Guid userId,
        bool seal,
        IEnumerable<string>? tags = null,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionRemoveFormTags)))
        {
            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                // load existing.
                var (instance, tc) = await _instances.LoadAsync(instanceId);
                instance.Guarantees().IsNotNull();


                // set data
                instance.LastModifier = userId;
                instance.UpdatedDate = DateTime.UtcNow;

                // tags
                if (tags is not null && tags.Any())
                {
                    await _tagger.Untag(trx!, instance, _instances, tags);

                    // events
                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.form_update_tags";
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

    private async Task HelpDeleteFormInstance(
        Guid id,
        AppEventOrigin? origin,
        string? action,
        Guid? userId,
        ITransactionContext trx,
        bool seal,
        string context,
        IEnumerable<string>? eventTags = null)
    {
        using (PerfTrack.Stopwatch(nameof(HelpDeleteFormInstance)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (existing,_) = await _instances.LoadAsync(id);
                if (existing is null)
                    return;


                await _instances.DeleteAsync(existing);

                await _comments.DeleteFilterAsync(trx!, c => c.HostEntity == id);

                await _tagger.Untag(trx!, existing, _instances, existing.Tags);
                
                var (files, _) = await _managedFiles.GetAllAsync(mf => mf.AttachedEntityId == id);
                var work = new List<Task<string>>();
                foreach(var file in files)
                {
                    work.Add(_managedFileLogic.DeleteFileAsync(file.Id, userId));
                }
                await Task.WhenAll(work);

                if(existing.AttachedSchedules.EmptyIfNull().Any())
                {
                    await _scheduler.DescheduleAsync(existing.AttachedSchedules);
                }

                _eventSink.BeginBatch(trx!);
                var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(existing.HostWorkSet!.Value, existing.HostWorkItem!.Value!);
                var topic = $"{workSetTemplateName}.{workItemTemplateName}.{existing.Template}.{context}.form_delete_instance";
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

    public async Task EventDeleteForm(
        Guid id,
        AppEventOrigin origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteForm)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteFormInstance(id, origin, null, null, trx!, seal, "event", null);

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

    public async Task ActionDeleteForm(
       Guid id,
       string action,
       Guid userId,
       bool seal = false,
       ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionDeleteForm)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                await HelpDeleteFormInstance(id, null, action, userId, trx!, seal, "action", null);

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

    // delete work item forms
    public async Task ActionDeleteWorkItemForms(
        Guid workItem,
        string action,
        Guid userId,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(ActionDeleteWorkItemForms)))
        {

            bool isSelfContained = trx is null;
            if (isSelfContained)
                trx = await _dataEnvironment.OpenTransactionAsync(CancellationToken.None);

            try
            {
                var (matching, _) = await _instances.GetAllAsync(fi=>fi.HostWorkItem == workItem);
                if (!matching.Any())
                    return;

                var work = new List<Task>();
                foreach(var item in matching)
                {
                    work.Add(HelpDeleteFormInstance(item.Id, null, action, userId, trx!, seal, "action", null));
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


    public async Task EventDeleteWorkItemForms(
        Guid workItem,
        AppEventOrigin origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteWorkItemForms)))
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
                    work.Add(HelpDeleteFormInstance(item.Id, origin, null, null, trx!, seal, "event", null));
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

    public async Task EventDeleteWorkItemForm(
        Guid workItem,
        Uri entityUri,
        AppEventOrigin? origin,
        bool seal = false,
        ITransactionContext? trx = null)
    {
        using (PerfTrack.Stopwatch(nameof(EventDeleteWorkItemForms)))
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

                await HelpDeleteFormInstance(id, origin, null, null, trx!, seal, "event", null);
                
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
    public async Task<FormInstanceViewModel> GetFormInstanceVM(Guid id, string tzid)
    {
        return await _formHelperLogic.GetFormInstanceVM(_content, id, tzid);
       
    }

    public async Task<FormInstance> GetRawFormInstance(Guid id)
    {
        using (PerfTrack.Stopwatch(nameof(GetFormInstanceVM)))
        {
            var (item, _) = await _instances.LoadAsync(id);
            item.Guarantees().IsNotNull();
            return item;
        }

    }

    public async Task<IList<FormInstanceViewModel>> GetFormInstances(Guid workItem, string tzid)
    {
        return await _formHelperLogic.GetFormInstances(_content, workItem, tzid);
        

    }

    public IList<FormTemplateViewModel> GetAllFormTemplates()
    {
        using(PerfTrack.Stopwatch(nameof(GetAllFormTemplates)))
        {
            lock (_formTemplateVMs)
            {
                if (!_formTemplateVMs.Any())
                    LoadFormTemplateVMs();

                return _formTemplateVMs.Values.ToIList();
            }


        }
    }

    private void LoadFormTemplateVMs()
    {
        _formHelperLogic.LoadFormTemplateVMs(_content, ref _formTemplateVMs);
        
    }

    public FormTemplateViewModel GetFormTemplateVM(string name)
    {
        using (PerfTrack.Stopwatch(nameof(GetFormTemplateVM)))
        {
            lock (_formTemplateVMs)
            {
                if (!_formTemplateVMs.Any())
                    LoadFormTemplateVMs();

                return _formTemplateVMs[name];
            }


        }
    }

    public FormTemplate? GetFormTemplate(string name)
    {
        return FormHelperLogic.GetFormTemplate(_content,name);
       
    }

    public async Task<Uri> GetFormInstanceRef(Guid id)
    {
        var instance = await GetRawFormInstance(id);
        instance.Guarantees().IsNotNull();
        return instance.MakeReference();
    }

    public async Task<List<EntitySummary>> GetTaggedFormInstanceRefs(Guid workItem, IEnumerable<string> tags)
    {
        using (PerfTrack.Stopwatch(nameof(GetTaggedFormInstanceRefs)))
        {
            var readyTags = TagUtil.MakeTags(tags);
            var (instances,_) = await _instances.
                GetAllAsync(fi => readyTags.Any(rt => fi.HostWorkItem==workItem && fi.Tags.Contains(rt)));

            var retval = new List<EntitySummary>();
            foreach(var instance in instances)
            {
                retval.Add(new EntitySummary
                {
                    EntityTemplate = instance.Template,
                    Uri = instance.MakeReference(false, true, null),
                    EntityType = instance.EntityType,
                    EntityTags = instance.Tags.ToList()
                });
            }

            return retval;

        }
    }
    #endregion



}
