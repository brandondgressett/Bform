using BFormDomain.CommonCode.Authorization;

namespace BFormDomain.CommonCode.Platform.ManagedFiles;

/// <summary>
/// This is the users viewmodel for the file removes any backend data we'd need elsewhere.
/// 
/// THIS MAKES IT SOUND LIKE THE VIEW MODEL PRESENTS OR DEPICTS A USER. THE WORDING IS AMBIGUOUS AND CONFUSING.
/// "THIS" is a A POOR SUBJECT.
/// 
/// The ManagedFileViewModel presents a managed file, presentable for the UI and easily digested by end-users.
/// </summary>
public class ManagedFileViewModel
{
    public Guid Id { get; set; }
    /// <summary>
    /// The original file name.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// A description of the file.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Keywords to describe the file for searches.
    /// 
    /// DON"T MIX UP TERMS. KEYWORDS ARE NOT TAGS. 
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// When the file was created.
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
    /// How many times the file has been downloaded.
    /// </summary>
    public int DownloadCount { get; set; } = 0;
    /// <summary>
    /// Who uploaded the file.
    /// </summary>
    public string? CreatorName { get; set; }
    /// <summary>
    /// Who/What was the last thing to edit the file.
    /// </summary>
    public string? LastModifier { get; set; }
    /// <summary>
    /// What type the file is.
    /// </summary>
    public string? FileType { get; set; }     
    
    /// <summary>
    /// What type is the attached entity
    /// </summary>
    public string? AttachedEntityType { get; set; }

    /// <summary>
    /// The id of the attached entity.
    /// </summary>
    public Guid? AttachedEntityId { get; set; }
    
    /// <summary>
    /// The workset this file is attached to.
    /// </summary>
    public Guid? HostWorkSet { get; set; }

    /// <summary>
    /// The work item this file is attached to.
    /// </summary>
    public Guid? HostWorkItem { get; set; }

    /// <summary>
    /// Converts the ManagedFile into a ManagedFileViewModel; which is a user friendly version of managedfile.
    /// </summary>
    /// <param name="data">The Managedfile backend object.</param>
    /// <param name="cache">Something to store the extra info for the meta data of the file.</param>
    /// <param name="timeZoneId">I DIDNT SEE ANY QUESTIONS ABOUT THIS PARAMETER</param>
    /// <returns>Returns the converted managedfileVIEWMODEL.</returns>
    public static async Task<List<ManagedFileViewModel>> Convert(
        IEnumerable<ManagedFileInstance> data,
        UserInformationCache cache,
        string timeZoneId)
    {
        var retval = new List<ManagedFileViewModel>();

        foreach(var managedFile in data)
        {
            ManagedFileViewModel vm = await Create(cache, timeZoneId, managedFile);
            retval.Add(vm);
        }

        return retval;

    }

    public static async Task<ManagedFileViewModel> Create(
        UserInformationCache cache, string timeZoneId, ManagedFileInstance managedFile)
    {
        string creatorUserName = string.Empty;
        string updaterUserName = string.Empty;
        if (managedFile.Creator is not null)
        {
            var userInfo = await cache.Fetch(managedFile.Creator.Value);
            if (userInfo is not null)
                creatorUserName = userInfo.UserName;
        }
        if (managedFile.LastModifier is not null)
        {
            var userInfo = await cache.Fetch(managedFile.LastModifier.Value);
            if (userInfo is not null)
                updaterUserName = userInfo.UserName;
        }


        var localTz = TimeZoneInfo.FromSerializedString(timeZoneId);
        var creationDate = TimeZoneInfo.ConvertTimeFromUtc(managedFile.CreatedDate, localTz);
        var modifiedDate = TimeZoneInfo.ConvertTimeFromUtc(managedFile.UpdatedDate, localTz);
        var lastDownload = TimeZoneInfo.ConvertTimeFromUtc(managedFile.LastDownload, localTz);

        var vm = new ManagedFileViewModel
        {
            Id = managedFile.Id,
            AttachedEntityId = managedFile.AttachedEntityId,
            AttachedEntityType = managedFile.AttachedEntityType,
            CreatedDate = creationDate,
            CreatorName = creatorUserName,
            Description = managedFile.Description,
            DownloadCount = managedFile.DownloadCount,
            FileType = managedFile.FileType,
            HostWorkItem = managedFile.HostWorkItem,
            HostWorkSet = managedFile.HostWorkSet,
            LastDownload = lastDownload,
            LastModifier = updaterUserName,
            OriginalFileName = managedFile.OriginalFileName,
            Tags = managedFile.Tags,
            UpdatedDate = modifiedDate
        };
        return vm;
    }
}
