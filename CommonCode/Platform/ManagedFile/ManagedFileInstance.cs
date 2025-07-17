using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.CommonCode.Platform.ManagedFile;
using BFormDomain.CommonCode.Platform.Scheduler;
using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.ManagedFiles;

/// <summary>
/// Describes an instance of a managed file.
/// </summary>
public class ManagedFileInstance : IDataModel, IAppEntity, IEntityAttachment
{

    [BsonId]
    public Guid Id { get; set; }
    /// <summary>
    /// Version of the file to help with multiple users modifying a file at once.
    /// </summary>
    public int Version { get; set; } = 0;
    public string? TenantId { get; set; }

    /// <summary>
    /// The original file name when uploaded.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the file in storage.
    /// </summary>
    public string StorageName { get; set; } = string.Empty;
    
    /// <summary>
    /// A description of what the file is.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Where the file is stored.
    /// </summary>
    public string ContainerName { get; set; } = "Global";
    
    /// <summary>
    /// Keywords to describe what file is for searches.
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();
    public List<string> AttachedSchedules { get; set; } = new();

    /// <summary>
    /// When it was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }
    
    /// <summary>
    /// When the file was messed with.
    /// </summary>
    public DateTime UpdatedDate { get; set; }
    
    /// <summary>
    /// The last time someone downloaded the file.
    /// </summary>
    public DateTime LastDownload { get; set; }
    
    /// <summary>
    /// When the file will be groomed.
    /// </summary>
    public DateTime? GroomByDate { get; set; } = null!;
    
    /// <summary>
    /// extends the grooming date by this amount when the file is accessed.
    /// </summary>
    public int LifespanDaysExtendOnAccess { get; set; } = 0;
    
    /// <summary>
    /// The amount of times this file has been downloaded.
    /// </summary>
    public int DownloadCount { get; set; } = 0;
    
    /// <summary>
    /// Who created this file.
    /// </summary>
    public Guid? Creator { get; set; }
    
    /// <summary>
    /// The last thing to edit the file.
    /// </summary>
    public Guid? LastModifier { get; set; }
    
    /// <summary>
    /// File type.
    /// </summary>
    public string EntityType 
    {
        get { return nameof(ManagedFileInstance); }
        set { }
    }

    /// <summary>
    /// A fancy way of storing file type needed for IAppEntity.
    /// </summary>
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// What type of file it is.
    /// </summary>
    public string FileType
    {
        get
        {
            return Template;
        }

        set
        {
            Template = value;
        }
    }

    /// <summary>
    /// The type of attach entities.
    /// </summary>
    public string? AttachedEntityType { get; set; }
    
    /// </summary>
    /// The ID of the attached entity.
    /// <summary>
    public Guid? AttachedEntityId { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public string? AttachedEntityTemplate { get; set; }
    
    /// <summary>
    /// File is attached to a workset.
    /// </summary>
    public Guid? HostWorkSet { get; set; }
    
    /// <summary>
    /// File is attached to a work item.
    /// </summary>
    public Guid? HostWorkItem { get; set; }

    public Uri MakeReference(bool template = false, bool vm = false, string? queryParameters = null)
    {
        return ManagedFileReferenceBuilderImplementation.MakeReference(Template, Id, template, vm, queryParameters);
    }

    /// <summary>
    /// Checks to see if the file is tagged.
    /// </summary>
    /// <param name="anyTags">Check for these specific tags.</param>
    /// <returns>True if the tags were found false otherwise.</returns>
    public bool Tagged(params string[] anyTags)
    {
        return anyTags.Any(at => Tags.Contains(TagUtil.MakeTag(at)));
    }


    /// <summary>
    /// Converts the file to JSON.
    /// </summary>
    /// <returns>The Jsonified file data.</returns>
    public JObject ToJson()
    {
        return JObject.FromObject(this);
    }

    


}
