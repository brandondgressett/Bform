namespace BFormDomain.CommonCode.Platform.ManagedFiles;
/// <summary>
/// What sort of things could happen to a file.
/// Helpful for auditing.
/// </summary>
public enum ManagedFileEvents
{
    /// <summary>
    /// EACH ITEM GETS A SUMMARY COMMENT
    /// </summary>
    Created,
    ContentUpdated,
    MetadataUpdated,
    Downloaded,
    Deleted,
    Groomed
}
