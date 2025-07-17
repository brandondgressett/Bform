
using BFormDomain.CommonCode.Authorization;
using BFormDomain.CommonCode.Platform.AppEvents;
using BFormDomain.CommonCode.Platform.Constants;
using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Utility;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Repository;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BFormDomain.CommonCode.Platform.ManagedFiles;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.ManagedFiles;


/// <summary>
/// The main file for managing file, Create update or delete files from here.
/// 
/// 
/// "THE MAIN FILE" -- poor subject. The file does nothing but sit there looking pretty and being edited.
/// The ManagegFileLogic COMPONENT exposes services to track, audit, and store files uploaded into the application.
/// </summary>
public class ManagedFileLogic
{
    /// <summary>
    /// An enum containing all of the different ways we would order our file data.
    /// </summary>
    public enum Ordering
    {
        DownloadCount,
        DownloadDate,
        ModifiedDate,
        GroomingDate
    }

    /// <summary>
    /// The repository for managing our files.
    /// </summary>
    private readonly IRepository<ManagedFileInstance> _fileRepo;

    /// <summary>
    /// The logger for debugging.
    /// </summary>
    private readonly ILogger<ManagedFileLogic> _logger;

    /// <summary>
    /// The alarm for the file system.
    /// </summary>
    private readonly IApplicationAlert _alerts;

    private readonly IManagedFilePersistence _fileStore;

    private readonly IDataEnvironment _dataClient;

    private readonly UserInformationCache _userCache;

    /// <summary>
    /// Where the file audits are stored.
    /// </summary>
    private readonly IRepository<ManagedFileAudit> _auditsRepo;

    /// <summary>
    /// All file types.
    /// </summary>
    private readonly TemplateNamesCache _tnCache;

    /// <summary>
    /// The event queue for passing info.
    /// </summary>
    private readonly AppEventSink _evSink;

    /// <summary>
    /// Tags files based on tags passed.
    /// </summary>
    private readonly Tagger _tagger;

    /// <summary>
    /// The amount of allowed errors before action is taken.
    /// </summary>
    private readonly int _errThreshold;

    /// <summary>
    /// If a file is audited.
    /// </summary>
    private readonly bool _isAudited;

    /// <summary>
    /// A list of our allowed file types.
    /// </summary>
    private readonly static IReadOnlyDictionary<string, string> _mimeTypes = 
        new Dictionary<string,string>()
    {
        { ".txt", "text/plain" },
        { ".pdf", "application/pdf" },
        { ".doc", "application/vnd.ms-word" },
        { ".docx", "application/vnd.ms-word" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".png", "image/png" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif", "image/gif" },
        { ".csv", "text/csv" },
        {".html", "text/html" }
    }; // TODO: Maybe read available types from a content file
    
    public ManagedFileLogic(
        IRepository<ManagedFileInstance> fileRepo,
        ILogger<ManagedFileLogic> logger,
        IApplicationAlert alerts,
        IManagedFilePersistence managedFilePersistence,
        IDataEnvironment dataEnvironment,
        IRepository<ManagedFileAudit> auditsRepo,
        TemplateNamesCache tnCache,
        UserInformationCache userCache,
        AppEventSink evSink,
        Tagger tagger,
        IOptions<ManagedFileStoreOptions> options)
    {
        _fileRepo = fileRepo;
        _logger = logger;
        _alerts = alerts;
        _fileStore = managedFilePersistence;
        _auditsRepo = auditsRepo;
        _dataClient = dataEnvironment;
        _tagger = tagger;
        var optVal = options.Value;
        _errThreshold = optVal.ErrorThreshold;
        _isAudited = optVal.IsAudited;
        _tnCache = tnCache;
        _evSink = evSink;
        _userCache = userCache;
    }

    /// <summary>
    /// Uploads a file to the repo.
    /// </summary>
    /// <param name="stream">The file data itself.</param>
    /// <param name="fileName">The File name duh!</param>
    /// <param name="description">A description of the file that was uploaded.</param>
    /// <param name="container">Where to upload the file.</param>
    /// <param name="tags">Keywords to help filtering for specific files.</param>
    /// <param name="creatorId">The user that uploaded the file's ID.</param>
    /// <param name="hostWorkSet"></param>
    /// <param name="hostWorkItem"></param>
    /// <param name="attachedEntityType">The file type.</param>
    /// <param name="attachedEntityId">The ID of the file.</param>
    /// <param name="attachedEntityTemplate">The type of entity attached.</param>
    /// <param name="groomByDate">When to groom this file.</param>
    /// <param name="extendOnAccess">How long to extend the groom date on access.</param>
    /// <param name="ct"></param>
    /// <returns>Returns the ID of the created file.</returns>
    public async Task<Guid> CreateAsync(
        Stream stream,
        string fileName, 
        string description,
        string container,
        IEnumerable<string>? tags,
        Guid? creatorId,
        Guid hostWorkSet,
        Guid hostWorkItem,
        string attachedEntityType,
        Guid attachedEntityId,
        string? attachedEntityTemplate = null,
        DateTime? groomByDate = null,
        int extendOnAccess = 0,
        CancellationToken ct = default)
    {
        using var trx = _dataClient.OpenTransaction(ct);

        try
        {
            fileName.Requires("The file name must be valid.").IsNotNullOrEmpty();
            description.Requires("Files require a valid description").IsNotNull();
            container.Requires("Files require a container name to place them into.").IsNotNullOrEmpty();

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            _mimeTypes.ContainsKey(ext).Requires().IsTrue();
            var mimeType= _mimeTypes[ext];

            string storageId = GuidEncoder.Encode(Guid.NewGuid())!;

            await _fileStore.UpsertAsync(container, storageId, stream, ct);

            var id = Guid.NewGuid();
            var fileRecord = new ManagedFileInstance
            {
                Id = id,
                Version = 0,
                OriginalFileName = fileName,
                StorageName = storageId,
                ContainerName = container,
                Tags = tags!.EmptyIfNull().ToList(),
                CreatedDate= DateTime.UtcNow,
                UpdatedDate= DateTime.UtcNow,
                LastDownload = DateTime.MinValue,
                GroomByDate = groomByDate,
                LifespanDaysExtendOnAccess = extendOnAccess,
                DownloadCount = 0,
                Creator = creatorId,
                Template = mimeType,
                AttachedEntityType = attachedEntityType,
                AttachedEntityId = attachedEntityId,
                AttachedEntityTemplate = attachedEntityTemplate,
                HostWorkSet = hostWorkSet,
                HostWorkItem = hostWorkItem
            };

            await _fileRepo.CreateAsync(trx, fileRecord);

            if (tags!.EmptyIfNull().Any())
                await _tagger.Tag(trx, fileRecord, _fileRepo, tags!);

            if(_isAudited)
            {
                await _auditsRepo.CreateAsync(trx,
                    new ManagedFileAudit
                {
                    Id = Guid.NewGuid(),
                    Version = 0,
                    Actor = creatorId,
                    DateTime = DateTime.UtcNow,
                    Event = ManagedFileEvents.Created,
                    ManagedFileId = id,
                    OriginalFileName= fileName,
                    Information = $"Created {stream.Length} bytes with mime type {fileRecord.Template}, attached to {attachedEntityId}"
                });
            }

            _evSink.BeginBatch(trx);
            var actionId = GuidEncoder.Encode(id);
            var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(hostWorkSet, hostWorkItem);
            string topic = $"{workSetTemplateName}.{workItemTemplateName}.{attachedEntityType}.{attachedEntityTemplate}.action.file_create";
            await _evSink.Enqueue(null, topic, actionId, fileRecord, creatorId, null, false);
            await _evSink.CommitBatch();

            await trx.CommitAsync(ct);
            return id;

        } catch (Exception ex)
        {
            await trx.AbortAsync(ct);
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }
    
    /// <summary>
    /// Updates the content of a file.
    /// </summary>
    /// <param name="id">The ID of the file.</param>
    /// <param name="user">The user updating the file.</param>
    /// <param name="sourceFileName">The name of the file.</param>
    /// <param name="stream">The new data for the file.</param>
    /// <param name="ct">Cancel</param>
    /// <returns>Returns an event ID.</returns>
    public async Task<string> UpdateFileContentAsync(
        Guid id,
        Guid? user,
        string sourceFileName,
        Stream stream,
        CancellationToken ct = default)
    {
        using var trx = _dataClient.OpenTransaction(ct);

        try
        {
            var (file, rc) = await _fileRepo.LoadAsync(id);
            file.Guarantees().IsNotNull();
            var storageId = file.StorageName;
            var container = file.ContainerName;

            var sourceType = Path.GetExtension(sourceFileName).ToLowerInvariant();
            _mimeTypes.ContainsKey(sourceType).Guarantees().IsTrue();
            var sourceTemplate = _mimeTypes[sourceType];
            sourceTemplate.Guarantees().IsEqualTo(file.Template);

            await _fileStore.UpsertAsync(container, storageId, stream, ct);

            file.UpdatedDate = DateTime.UtcNow;
            if(file.GroomByDate is not null && file.LifespanDaysExtendOnAccess > 0)
            {
                file.GroomByDate = DateTime.UtcNow + TimeSpan.FromDays(file.LifespanDaysExtendOnAccess);
            }

            file.LastModifier = user;

            await _fileRepo.UpdateAsync(trx, (file, rc)); 

            if (_isAudited)
            {
                await _auditsRepo.CreateAsync(trx,
                    new ManagedFileAudit
                    {
                        Id = Guid.NewGuid(),
                        Version = 0,
                        Actor = user,
                        DateTime = DateTime.UtcNow,
                        Event = ManagedFileEvents.ContentUpdated,
                        ManagedFileId = id,
                        OriginalFileName = file.OriginalFileName,
                        Information = $"Updated with {stream.Length} bytes."
                    });
            }

            _evSink.BeginBatch(trx);
            var actionId = GuidEncoder.Encode(Guid.NewGuid());

            var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(file.HostWorkSet!.Value, file.HostWorkItem!.Value);
            string topic = $"{workSetTemplateName}.{workItemTemplateName}.{file.AttachedEntityType}.{file.AttachedEntityTemplate}.action.file_update_content";
            await _evSink.Enqueue(null, topic, actionId, file, user, null, false);
            await _evSink.CommitBatch();

            await trx.CommitAsync(ct);

            return actionId!;

        } catch (Exception ex)
        {
            await trx.AbortAsync(ct);
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Updates the description, and/or the tags on the file.
    /// </summary>
    /// <param name="id">The ID of the file.</param>
    /// <param name="user">The user updating the file.</param>
    /// <param name="description">The new description of the file.</param>
    /// <param name="addTags">The new keywords that help find the file.</param>
    /// <param name="removeTags">The tags you want removed from the file.</param>
    /// <param name="ct"></param>
    /// <returns>Returns an event ID.</returns>
    public async Task<string> UpdateFileMetadataAsync(
        Guid id,
        Guid? user,
        string description,
        IEnumerable<string>? addTags = null,
        IEnumerable<string>? removeTags = null,
        CancellationToken ct = default)
    {
        using var trx = _dataClient.OpenTransaction(ct);

        try
        { 
            var (file, rc) = await _fileRepo.LoadAsync(id);
            file.Guarantees().IsNotNull();

            file.UpdatedDate = DateTime.UtcNow;
            if (file.GroomByDate is not null && file.LifespanDaysExtendOnAccess > 0)
            {
                file.GroomByDate = DateTime.UtcNow + TimeSpan.FromDays(file.LifespanDaysExtendOnAccess);
            }

            file.LastModifier = user;

            file.Description = description;
            if(addTags is not null && addTags.Any())
            {
                await _tagger.Tag(trx, file, _fileRepo, addTags!);
            }

            if(removeTags is not null && removeTags.Any())
            {
                await _tagger.Untag(trx, file, _fileRepo, removeTags!);
            }

            await _fileRepo.UpdateAsync(trx, (file, rc));

            if (_isAudited)
            {
                await _auditsRepo.CreateAsync(trx,
                    new ManagedFileAudit
                    {
                        Id = Guid.NewGuid(),
                        Version = 0,
                        Actor = user,
                        DateTime = DateTime.UtcNow,
                        Event = ManagedFileEvents.MetadataUpdated,
                        ManagedFileId = id,
                        OriginalFileName = file.OriginalFileName,
                        Information = $"Updated with description {description}, tags {string.Join(',',file.Tags)}."
                    });
            }

            _evSink.BeginBatch(trx);
            var actionId = GuidEncoder.Encode(Guid.NewGuid());

            var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(file.HostWorkSet!.Value, file.HostWorkItem!.Value);
            string topic = $"{workSetTemplateName}.{workItemTemplateName}.{file.AttachedEntityType}.{file.AttachedEntityTemplate}.action.file_update_metadata";
            await _evSink.Enqueue(null, topic, actionId, file, user, null, false);
            await _evSink.CommitBatch();


            await trx.CommitAsync(ct);

            return actionId!;
        }
        catch (Exception ex)
        {
            await trx.AbortAsync(ct);
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }

    }

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="id">The ID of the file to be deleted.</param>
    /// <param name="user">The user deleting a file.</param>
    /// <param name="ct">cancel</param>
    /// <returns>Returns an event ID.</returns>
    public async Task<string> DeleteFileAsync(
        Guid id,
        Guid? user,
        CancellationToken ct = default)
    {
        using var trx = _dataClient.OpenTransaction(ct);

        try
        {
            var (file, rc) = await _fileRepo.LoadAsync(id);
            file.Guarantees().IsNotNull();

            var storageId = file.StorageName;
            var container = file.ContainerName;
            await _fileStore.DeleteAsync(container, storageId, ct);

            if(file.Tags.Any())
            {
                // update the tag counts
                await _tagger.Untag(trx, file, _fileRepo, file.Tags);
            }

            await _fileRepo.DeleteAsync(trx, (file, rc));

            if (_isAudited)
            {
                await _auditsRepo.CreateAsync(trx,
                    new ManagedFileAudit
                    {
                        Id = Guid.NewGuid(),
                        Version = 0,
                        Actor = user,
                        DateTime = DateTime.UtcNow,
                        Event = ManagedFileEvents.Deleted,
                        ManagedFileId = id,
                        OriginalFileName = file.OriginalFileName,
                        Information = ""
                    });
            }

            _evSink.BeginBatch(trx);
            var actionId = GuidEncoder.Encode(Guid.NewGuid());

            var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(file.HostWorkSet!.Value, file.HostWorkItem!.Value);
            string topic = $"{workSetTemplateName}.{workItemTemplateName}.{file.AttachedEntityType}.{file.AttachedEntityTemplate}.action.file_deleted";
            await _evSink.Enqueue(null, topic, actionId, file, user, null, false);
            await _evSink.CommitBatch();

            await trx.CommitAsync(ct);

            return actionId!;
        }
        catch (Exception ex)
        {
            await trx.AbortAsync(ct);
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Deletes a file completely from the system after a grooming date.
    /// </summary>
    /// <param name="id">The ID of the file.</param>
    /// <param name="ct">Cancel</param>
    /// <returns>Returns a task for something else to async.</returns>
    public async Task GroomFileAsync(
        Guid id,
        CancellationToken ct = default)
    {
        using var trx = _dataClient.OpenTransaction(ct);

        try
        {
            var (file, rc) = await _fileRepo.LoadAsync(id);
            file.Guarantees().IsNotNull();

            var storageId = file.StorageName;
            var container = file.ContainerName;
            await _fileStore.DeleteAsync(container, storageId, ct);

            if (file.Tags.Any())
            {
                // update the tag counts
                await _tagger.Untag(trx, file, _fileRepo, file.Tags);
            }

            await _fileRepo.DeleteAsync(trx, (file, rc));

            if (_isAudited)
            {
                await _auditsRepo.CreateAsync(trx,
                    new ManagedFileAudit
                    {
                        Id = Guid.NewGuid(),
                        Version = 0,
                        Actor = BuiltIn.SystemUser,
                        DateTime = DateTime.UtcNow,
                        Event = ManagedFileEvents.Groomed,
                        ManagedFileId = id,
                        OriginalFileName = file.OriginalFileName,
                        Information = $"System groomed file at {DateTime.UtcNow} utc."
                    });
            }

            _evSink.BeginBatch(trx);
            var actionId = GuidEncoder.Encode(Guid.NewGuid());

            var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(file.HostWorkSet!.Value, file.HostWorkItem!.Value);
            string topic = $"{workSetTemplateName}.{workItemTemplateName}.{file.AttachedEntityType}.{file.AttachedEntityTemplate}.action.file_groomed";
            await _evSink.Enqueue(new AppEventOrigin(nameof(ManagedFileGroomingService), null, null), topic, null, file, null, null, false);
            await _evSink.CommitBatch();


            await trx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await trx.AbortAsync(ct);
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Downloads the file.
    /// </summary>
    /// <param name="id">File ID</param>
    /// <param name="user">The user downloading the file; for the auditing process.</param>
    /// <param name="ct"></param>
    /// <returns>The file data, and file template.</returns>
    public async Task<(Stream, string)> Download(
        Guid id,
        Guid? user,
        CancellationToken ct)
    {
        using var trx = _dataClient.OpenTransaction(ct);

        try
        {
            var (file, rc) = await _fileRepo.LoadAsync(id);
            file.Guarantees($"Could not find file with id {id}.").IsNotNull();
            var storageId = file.StorageName;
            var container = file.ContainerName;

            var stream = await _fileStore.RetrieveAsync(container, storageId, ct);

            file.DownloadCount += 1;
            file.LastDownload = DateTime.UtcNow;

            await _fileRepo.UpdateAsync(trx, (file, rc));

            if (_isAudited)
            {
                await _auditsRepo.CreateAsync(trx,
                    new ManagedFileAudit
                    {
                        Id = Guid.NewGuid(),
                        Version = 0,
                        Actor = user,
                        DateTime = DateTime.UtcNow,
                        Event = ManagedFileEvents.Downloaded,
                        ManagedFileId = id,
                        OriginalFileName = file.OriginalFileName,
                        Information = $"File downloaded {file.DownloadCount} times."
                    });
            }

            _evSink.BeginBatch(trx);
            var actionId = GuidEncoder.Encode(Guid.NewGuid());

            var (workSetTemplateName, workItemTemplateName) = await _tnCache.GetTemplateNames(file.HostWorkSet!.Value, file.HostWorkItem!.Value);
            string topic = $"{workSetTemplateName}.{workItemTemplateName}.{file.AttachedEntityType}.{file.AttachedEntityTemplate}.action.file_downloaded";
            await _evSink.Enqueue(null, topic, actionId, file, user, null, false);
            await _evSink.CommitBatch();

            await trx.CommitAsync(ct);
            return (stream, file.Template);
        }
        catch (Exception ex)
        {
            await trx.AbortAsync(ct);
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }

    }

    /// <summary>
    /// Reorders file data.
    /// </summary>
    /// <param name="data">The data that you want to reorder.</param>
    /// <param name="ordering">The type of reordering you want.</param>
    private static void Reorder(List<ManagedFileInstance> data, Ordering ordering)
    {
        // we could ask the repo to order the data,
        // but there won't be all that many files, and this is easier to read
        // than passing around switchable vars of Expression<Func<T,TField>> for ordering
        switch (ordering)
        {
            case Ordering.DownloadCount:
                data = data.OrderByDescending(mf => mf.DownloadCount).ToList();
                break;
            case Ordering.ModifiedDate:
                data = data.OrderByDescending(mf => mf.UpdatedDate).ToList();
                break;
            case Ordering.GroomingDate:
                data = data.OrderBy(mf => mf.GroomByDate).ToList();
                break;
            case Ordering.DownloadDate:
                data = data.OrderByDescending(mf => mf.LastDownload).ToList();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Gets the file by name.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <returns>The file you got.</returns>
    public async Task<ManagedFileInstance?> GetFileAsync(string fileName)
    {
        var (file,_) = await _fileRepo.GetOneAsync(mf=>mf.OriginalFileName == fileName);
        return file;
    }

    /// <summary>
    /// Gets the file by ID.
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <returns>The file you got.</returns>
    public async Task<ManagedFileInstance?> GetFileAsync(Guid id)
    {
        var (file,_) = await _fileRepo.LoadAsync(id);
        return file;
    }

    public async Task<ManagedFileViewModel?> GetFileVMAsync(Guid id, string tzid)
    {
        var (file, _) = await _fileRepo.LoadAsync(id);
        return await ManagedFileViewModel.Create(_userCache, tzid, file);
    }

    /// <summary>
    /// Get files attached to an entity.
    /// </summary>
    /// <param name="entityId">The entity you are unpacking files from.</param>
    /// <param name="page">How many pages of the item have you gone through.</param>
    /// <param name="ordering">The type of ordering you want the set of files to be in.</param>
    /// <returns>The list of files unpacked from the entity.</returns>
    public async Task<IList<ManagedFileInstance>> GetAttachedFiles(Guid entityId, int page = 0, Ordering ordering = Ordering.ModifiedDate)
    {
        try
        {
            var (files, rc) = await _fileRepo.GetPageAsync(page,mf=>mf.AttachedEntityId == entityId);
            
            Reorder(files.EmptyIfNull().ToList(), ordering);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    public async Task<IList<ManagedFileInstance>> GetAllAttachedFiles(Guid entityId)
    {
        try
        {
            var (files, rc) = await _fileRepo.GetAllAsync(mf => mf.AttachedEntityId == entityId);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// This gets attached files.
    /// </summary>
    /// <param name="entityType">The entity you are getting the attachments</param>
    /// <param name="entityTemplate"></param>
    /// <param name="page">How many pages you've gone through.</param>
    /// <param name="ordering">The ordering type for the data.</param>
    /// <returns>The Attached Files</returns>
    public async Task<IList<ManagedFileInstance>> GetAttachedTypeAsync(
        string entityType, string entityTemplate, int page = 0, Ordering ordering = Ordering.ModifiedDate)
    {
        try
        {
            var (files, rc) = await _fileRepo.GetPageAsync(page, mf => mf.AttachedEntityType == entityType && 
                                                                 mf.AttachedEntityTemplate == entityTemplate);
            files.Guarantees($"Could not find any file with entity type {entityType} and template {entityTemplate}.").IsNotNull();
            Reorder(files, ordering);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Gets files based on keywords(tags) to filter on as long as it has any of the tags.
    /// </summary>
    /// <param name="tags">Keywords that filter the files out.</param>
    /// <param name="page">How many pages you've gone through.</param>
    /// <param name="ordering">How the data is organized.</param>
    /// <returns>The filtered files.</returns>
    public async Task<IList<ManagedFileInstance>> GetFilesByAnyTag(IEnumerable<string> tags, int page = 0, Ordering ordering = Ordering.ModifiedDate)
    {
        try
        {
            tags.Requires("There weren't any files matching your null tag.").IsNotNull();
            tags.Requires("There weren't any files with an empty tag.").IsNotEmpty();

            var (files, rc) = await _fileRepo.GetPageAsync(page, mf => tags.Any(tg=>mf.Tags.Contains(tg)));
            files.Guarantees().IsNotNull();
            Reorder(files, ordering);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Gets files based on all tags given.
    /// </summary>
    /// <param name="tags">Keywords to filter the files.</param>
    /// <param name="page">How many pages you've been through.</param>
    /// <param name="ordering">How the data is organized.</param>
    /// <returns>The filtered files.</returns>
    public async Task<IList<ManagedFileInstance>> GetFilesByAllTags(IEnumerable<string> tags, int page = 0, Ordering ordering = Ordering.ModifiedDate)
    {
        try
        {
            tags.Requires("There weren't any files matching your null tag.").IsNotNull();
            tags.Requires("There weren't any files with an empty tag.").IsNotEmpty();

            var (files, rc) = await _fileRepo.GetPageAsync(page, mf => tags.All(tg => mf.Tags.Contains(tg)));
            files.Guarantees().IsNotNull();
            Reorder(files, ordering);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Gets files of a certain type, and has any of the listed tags.
    /// </summary>
    /// <param name="ext">File type.</param>
    /// <param name="tags">How to filter the data.</param>
    /// <param name="page">How many pages you've gone through.</param>
    /// <param name="ordering">How the data is organized.</param>
    /// <returns>The filtered files.</returns>
    public async Task<IList<ManagedFileInstance>> GetTypedFilesByAnyTag(string ext, IEnumerable<string> tags, int page = 0, Ordering ordering = Ordering.ModifiedDate)
    {
        try
        {
            tags.Requires("There weren't any files matching your null tag.").IsNotNull();
            tags.Requires("There weren't any files with an empty tag.").IsNotEmpty();

            _mimeTypes.ContainsKey(ext).Requires().IsTrue();
            var mimeType = _mimeTypes[ext];

            var (files, rc) = await _fileRepo.GetPageAsync(page, mf => mf.Template == mimeType && tags.Any(tg => mf.Tags.Contains(tg)));
            files.Guarantees().IsNotNull();
            Reorder(files, ordering);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Gets a specific file type by all provided tags.
    /// </summary>
    /// <param name="ext">File type.</param>
    /// <param name="tags">Tags to filter the files by.</param>
    /// <param name="page">How many pages your've been through.</param>
    /// <param name="ordering"></param>
    /// <returns>The filtered files.</returns>
    public async Task<IList<ManagedFileInstance>> GetTypedFilesByAllTags(string ext, IEnumerable<string> tags, int page = 0, Ordering ordering = Ordering.ModifiedDate)
    {
        try
        {
            tags.Requires("There weren't any files matching your null tag.").IsNotNull();
            tags.Requires("There weren't any files with an empty tag.").IsNotEmpty();
            _mimeTypes.ContainsKey(ext).Requires().IsTrue();
            var mimeType = _mimeTypes[ext];

            var (files, rc) = await _fileRepo.GetPageAsync(page, mf => tags.All(tg => mf.Template == mimeType && mf.Tags.Contains(tg)));
            files.Guarantees().IsNotNull();
            Reorder(files, ordering);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Gets files by type.
    /// </summary>
    /// <param name="ext">File type.</param>
    /// <param name="page">How many pages you've been through.</param>
    /// <param name="ordering">How the data is ordered.</param>
    /// <returns>The typed files.</returns>
    public async Task<IList<ManagedFileInstance>> GetTypedFiles(string ext, int page = 0, Ordering ordering = Ordering.ModifiedDate)
    {
        try
        {
            _mimeTypes.ContainsKey(ext).Requires($"files of type {ext} aren't allowed.").IsTrue();
            var mimeType = _mimeTypes[ext];

            var (files, rc) = await _fileRepo.GetPageAsync(page, mf => mf.Template == mimeType);
            files.Guarantees().IsNotNull();
            Reorder(files, ordering);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Gets all files.
    /// </summary>
    /// <param name="page">Page to load.</param>
    /// <param name="ordering">Sorting method.</param>
    /// <returns>All of them hahahahhahahhahaha!</returns>
    public async Task<IList<ManagedFileInstance>> GetAllFiles(int page = 0, Ordering ordering = Ordering.ModifiedDate)
    {
        try
        {
            var (files, rc) = await _fileRepo.GetPageAsync(page);
            files.Guarantees().IsNotNull();
            Reorder(files, ordering);

            return files;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services,
                LogLevel.Information, ex.TraceInformation(),
                _errThreshold, nameof(ManagedFileLogic));
            throw;
        }
    }

    /// <summary>
    /// Gets all the files past their grooming date.
    /// </summary>
    /// <param name="page">Page to load.</param>
    /// <returns>All files that are past their groom date.</returns>
    public async Task<IList<ManagedFileInstance>> GetGroomableFiles(int page)
    {
        var (files,_) = await _fileRepo.GetOrderedPageAsync(
            mf => mf.GroomByDate, descending: false, page,
            mf => mf.GroomByDate < DateTime.UtcNow);
        return files;
    }

    /// <summary>
    /// Gets the history of the file by ID.
    /// </summary>
    /// <param name="id">Filed ID.</param>
    /// <param name="page">How many pages you've been through.</param>
    /// <param name="start">The start date of history.</param>
    /// <param name="end">The end date of history.</param>
    /// <returns>All of the audits for the file in between the dates provided.</returns>
    public async Task<IList<ManagedFileAudit>> GetAuditForFile(Guid id, int page,
        DateTime start, DateTime end)
    {
        var (audits, _) = await _auditsRepo.GetOrderedPageAsync(
            audit=>audit.DateTime, descending:false,
            page,
            audit=>audit.ManagedFileId == id && audit.DateTime >= start && audit.DateTime <= end);
        
        return audits;
    }

    /// <summary>
    /// Gets the audits of files by user.
    /// </summary>
    /// <param name="page">Page to load.</param>
    /// <param name="start">The start date of the history.</param>
    /// <param name="end">The end date of the history.</param>
    /// <param name="user">The user that had audited the file.</param>
    /// <returns>The list of audits.</returns>
    public async Task<IList<ManagedFileAudit>> GetAuditForFile(int page,
        DateTime start, DateTime end, Guid user)
    {
        var (audits, _) = await _auditsRepo.GetOrderedPageAsync(
            audit=>audit.DateTime, descending: false,
            page,
            audit => 
                audit.Actor == user &&
                audit.DateTime >= start && audit.DateTime <= end);
        
        return audits;
    }

    /// <summary>
    /// Gets the audits for an event.
    /// </summary>
    /// <param name="page">Page to load.</param>
    /// <param name="start">The start date of the audits.</param>
    /// <param name="end">The end date of the audits.</param>
    /// <param name="event">Which event to search for audits on.</param>
    /// <returns>All audits between the two dates.</returns>
    public async Task<IList<ManagedFileAudit>> GetAuditForFile(int page,
        DateTime start, DateTime end, ManagedFileEvents @event)
    {
        var (audits, _) = await _auditsRepo.GetOrderedPageAsync(
            audit => audit.DateTime, descending: false,
            page,
            audit =>
                audit.Event == @event &&
                audit.DateTime >= start && audit.DateTime <= end);
        
        return audits;
    }


}

