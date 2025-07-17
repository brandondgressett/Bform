namespace BFormDomain.CommonCode.Platform.ManagedFiles;



/// <summary>
/// DO YOU KNOW WHAT OPTIONS CLASSES ARE AND WHAT THEY DO?
/// 
/// 
/// </summary>
public class ManagedFileStoreOptions
{
    /// <summary>
    /// How many errors have to happen before we take action.
    /// </summary>
    public int ErrorThreshold { get; set; } = 15;

    /// <summary>
    /// If the file is audited.
    /// </summary>
    public bool IsAudited { get; set; } = true;
}
