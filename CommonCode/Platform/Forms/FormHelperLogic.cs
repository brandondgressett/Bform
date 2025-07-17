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
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Forms
{
    public class FormHelperLogic
    {
        private readonly IApplicationAlert _alerts;
        private readonly IDataEnvironment _dataEnvironment;
        private readonly IRepository<FormInstance> _instances;
        private readonly Tagger _tagger;
        private readonly IApplicationTerms _terms;
        private readonly AppEventSink _eventSink;
        private readonly TemplateNamesCache _tnCache;
        private readonly BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic _scheduler;
        private readonly ILogger<FileApplicationPlatformContent> _logger;
        private readonly UserInformationCache _userCache;
        private readonly IEnumerable<IContentDomainSource> contentDomainSources;
        private readonly IEnumerable<IEntityInstanceLogic> instanceConsumers;
        private readonly IOptions<FileApplicationPlatformContentOptions> options;
        private readonly IRepository<Comment> _comments;
        private readonly IRepository<ManagedFileInstance> _managedFiles;
        private readonly ConcurrentDictionary<string, FormTemplateViewModel> _formTemplateVMs = new();

        public FormHelperLogic(IApplicationAlert alerts,
                    IDataEnvironment dataEnvironment,
                    IRepository<FormInstance> formInstances,
                    ILogger<FileApplicationPlatformContent> logger,
                    TemplateNamesCache tnCache,
                    Tagger tagger,
                    IApplicationTerms terms,
                    IRepository<Comment> comments,
                    IRepository<ManagedFileInstance> files,
                    BFormDomain.CommonCode.Platform.Scheduler.QuartzImplementation.QuartzISchedulerLogic scheduler,
                    AppEventSink sink,
                    UserInformationCache userCache,
                    IEnumerable<IContentDomainSource> contentDomainSources,
                    IEnumerable<IEntityInstanceLogic> instanceConsumers,
                    IOptions<FileApplicationPlatformContentOptions> options)
        {
            _logger = logger;
            _alerts = alerts;
            _dataEnvironment = dataEnvironment;
            _instances = formInstances;
            _tagger = tagger;
            _terms = terms;
            _scheduler = scheduler;
            _eventSink = sink;
            _tnCache = tnCache;
            _userCache = userCache;
            this.contentDomainSources = contentDomainSources;
            this.instanceConsumers = instanceConsumers;
            this.options = options;
            _comments = comments;
            _managedFiles = files;
        }


        
        public static void ValidateFormContentUsingSchema(IApplicationPlatformContent content,string contentJson, FormInstance instance)
        {
            // validate with schema.
            var template = content.GetContentByName<FormTemplate>(instance.Template);
            template.Guarantees().IsNotNull();
            var schemaJson = template!.ContentSchema.Json;//ContentSchema is null somehow
            schemaJson.Guarantees().IsNotNull();
            var jSchema = JSchema.Parse(schemaJson!.ToString());
            var contentObject = JObject.Parse(contentJson);
            contentObject.IsValid(jSchema).Guarantees().IsTrue();
        }


        public async Task<(Guid, string?, FormTemplateViewModel)> HelpCreateFormInstance(
            IApplicationPlatformContent content,
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
            var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(workSet, workItem);

            var id = Guid.NewGuid();

            // get the form template and content schema

            var template = content.GetContentByName<FormTemplate>(templateName)!;
            template.Guarantees().IsNotNull();

            var formTemplateVM = FormTemplateViewModel.Create(template, _terms);

            // create initial content

            if (!string.IsNullOrWhiteSpace(initialPropertiesName))
            {
                initialPropertiesJson = content.GetFreeJson(initialPropertiesName);
                initialPropertiesJson.Guarantees().IsNotNullOrEmpty();
            }
            else if (string.IsNullOrWhiteSpace(initialPropertiesJson))
            {
                var schema = JSchema.Parse(formTemplateVM.ContentSchema);
                initialPropertiesJson = JsonFromSchema.Generate(schema)?.ToString();
            }

            var initialContent = initialPropertiesJson is not null ?
                                    BsonDocument.Parse(initialPropertiesJson) :
                                    new BsonDocument();

            // create a form instance
            var readyTags = TagUtil.MakeTags(initialTags.EmptyIfNull());

            var formInstance = new FormInstance
            {
                Id = id,
                Version = 0,
                Template = templateName,
                CreatedDate = DateTime.UtcNow,
                Creator = userId,
                LastModifier = userId,
                UpdatedDate = DateTime.UtcNow,
                HostWorkSet = workSet,
                HostWorkItem = workItem,
                Home = home,
                Content = initialContent,
                Tags = new(readyTags)
            };

            if (!string.IsNullOrWhiteSpace(initialPropertiesName))
                ValidateFormContentUsingSchema(content, initialPropertiesJson!, formInstance);

            if (!formTemplateVM.EventsOnly)
            {
                if (template.Schedules.EmptyIfNull().Any())
                {
                    var originalJson = formInstance.ToJson();

                    foreach (var schedule in template.Schedules)
                    {
                        var scheduleId = await
                            _scheduler.EventScheduleEventsAsync(schedule,
                            originalJson,
                            workSet, workItem,
                            formInstance.Id.ToString(), readyTags);

                        formInstance.AttachedSchedules.Add(scheduleId.JobId);//CAG Change need to set scheduleID correctly currently just fixing syntax

                    }

                }

                await _instances.CreateAsync(trx!, formInstance);

                // count initial tags
                var work = new List<Task>();
                foreach (var tag in readyTags)
                    work.Add(_tagger.CountTags(formInstance, trx, 1, tag));
                await Task.WhenAll(work);
            }

            // send out events
            _eventSink.BeginBatch(trx!);
            var topic = $"{workSetTemplateName}.{workItemTemplateName}.{templateName}.{context}.form_create_instance";
            await _eventSink.Enqueue(origin, topic, action, formInstance, userId, eventTags, seal);
            await _eventSink.CommitBatch();

            return (id, initialPropertiesJson, formTemplateVM);
        }


        public async Task ActionInvokeCustomAction(
                        IApplicationPlatformContent content,
                        string action,
                        int buttonId,
                        Guid instanceId,
                        Guid userId,
                        ITransactionContext? trx = null)
        {
            using (PerfTrack.Stopwatch(nameof(ActionInvokeCustomAction)))
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

                    var template = content.GetContentByName<FormTemplate>(instance.Template)!;
                    template.Guarantees().IsNotNull();

                    template.ActionButtons.Any().Guarantees().IsTrue();
                    var matching = template.ActionButtons.First(it => it.Id == buttonId);


                    var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(instance.HostWorkSet!.Value, instance.HostWorkItem!.Value);
                    _eventSink.BeginBatch(trx!);
                    string topic = $"{workSetTemplateName}.{workItemTemplateName}.{instance.Template}.event.form_action.{matching.EventTopic}";
                    await _eventSink.Enqueue(null, topic, action, instance, userId, null, false);
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

        public async Task<FormInstanceViewModel> GetFormInstanceVM(IApplicationPlatformContent content,Guid id, string tzid)
        {
            using (PerfTrack.Stopwatch(nameof(GetFormInstanceVM)))
            {
                var (item, _) = await _instances.LoadAsync(id);
                item.Guarantees().IsNotNull();

                var localTz = TimeZoneInfo.FromSerializedString(tzid);

                var (comments, _) = await _comments.GetAllAsync(c => c.HostType == nameof(FormInstance) && c.HostEntity == item.Id);
                var (files, _) = await _managedFiles.GetAllAsync(mf => mf.AttachedEntityId == item.Id);
                var template = content.GetContentByName<FormTemplate>(item.Template)!;


                var retval = new FormInstanceViewModel
                {
                    Id = item.Id,
                    Created = item.CreatedDate,
                    Creator = item.Creator,
                    Home = item.Home,
                    JsonContent = item.JsonContent!.ToString(),
                    Tags = item.Tags,
                    Template = FormTemplateViewModel.Create(template, _terms),
                    WorkSet = item.HostWorkSet,
                    WorkItem = item.HostWorkItem,
                    LastModified = TimeZoneInfo.ConvertTimeFromUtc(item.UpdatedDate, localTz),
                    Comments = await CommentViewModel.Convert(comments, _userCache, tzid),
                    Files = await ManagedFileViewModel.Convert(files, _userCache, tzid)
                };

                return retval;
            }

        }

        public async Task<IList<FormInstanceViewModel>> GetFormInstances(IApplicationPlatformContent content, Guid workItem, string tzid)
        {
            using (PerfTrack.Stopwatch(nameof(GetFormInstances)))
            {
                var (items, _) = await _instances.GetAllAsync(fi => fi.HostWorkItem == workItem);
                var localTz = TimeZoneInfo.FromSerializedString(tzid);

                var retval = new List<FormInstanceViewModel>();
                foreach (var item in items)
                {
                    var (comments, _) = await _comments.GetAllAsync(c => c.HostType == nameof(FormInstance) && c.HostEntity == item.Id);
                    var (files, _) = await _managedFiles.GetAllAsync(mf => mf.AttachedEntityId == item.Id);
                    var template = content.GetContentByName<FormTemplate>(item.Template)!;

                    retval.Add(new FormInstanceViewModel
                    {
                        Id = item.Id,
                        Created = item.CreatedDate,
                        Creator = item.Creator,
                        Home = item.Home,
                        JsonContent = item.JsonContent!.ToString(),
                        Tags = item.Tags,
                        Template = FormTemplateViewModel.Create(template, _terms),
                        WorkSet = item.HostWorkSet,
                        WorkItem = item.HostWorkItem,
                        LastModified = TimeZoneInfo.ConvertTimeFromUtc(item.UpdatedDate, localTz),
                        Comments = await CommentViewModel.Convert(comments, _userCache, tzid),
                        Files = await ManagedFileViewModel.Convert(files, _userCache, tzid),
                    });
                }

                return retval;
            }

        }

        public void LoadFormTemplateVMs(IApplicationPlatformContent content, ref ConcurrentDictionary<string, FormTemplateViewModel> formTemplateVMs)
        {
            var templates = content.GetAllContent<FormTemplate>();
            foreach (var t in templates)
            {
                var vm = FormTemplateViewModel.Create(t, _terms);
                formTemplateVMs[t.Name] = vm;
                //_formTemplateVMs[t.Name] = vm;
            }
        }

        public static FormTemplate? GetFormTemplate(IApplicationPlatformContent content, string name)
        {
            return content.GetContentByName<FormTemplate>(name);
        }
    }
}
