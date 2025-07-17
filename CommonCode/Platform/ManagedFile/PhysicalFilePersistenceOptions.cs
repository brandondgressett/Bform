namespace BFormDomain.CommonCode.Platform.ManagedFiles;

/// <summary>
/// Bind these options from configuration to steer the behavior
/// of the PhysicalFilePersistence component.
/// </summary>
public class PhysicalFilePersistenceOptions
{
    /// <summary>
    /// Where to store the files.
    /// Put this in a folder marked non-execute, away from the application folder.
    /// For robust operation, store on a cloud drive or raid cluster.
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// How much exception shenanigans we'll accept before raising an alert.
    /// </summary>
    public int ErrorThreshold { get; set; } = 15;

    /// <summary>
    /// Max file size to upload, default is 100MB.
    /// </summary>
    public long MaximumBytes { get; set; } = 1024 * 1024 * 100;
}
