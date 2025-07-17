namespace BFormDomain.CommonCode.Platform.ManagedFiles;
/// <summary>
/// The IManagedFilePersistence interface specifies a way to store and retrieve file contents
/// from a persistent storage implentation. Eg, cloud blob storage, physical file storage, ftp storage ....
/// The ManagedFileLogic component uses IManagedFilePersistence to store file contents and
/// make it available for upload and download.
/// </summary>
public interface IManagedFilePersistence
{
    /// <summary>
    /// Updates or creates file data from the given stream into the persistence file store.
    /// </summary>
    /// <param name="container">Folder </param>
    /// <param name="id">The file Id. Must be unique among all the files in the system..</param>
    /// <param name="stream">The file data.</param>
    /// <param name="ct">Cancellation token to interrupt the method.</param>
    /// <returns></returns>
    Task UpsertAsync(string container, string id, Stream stream, CancellationToken ct);

    /// <summary>
    /// Gets a file.
    /// </summary>
    /// <param name="container">Where the file is.</param>
    /// <param name="id">The file ID.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Stream> RetrieveAsync(string container, string id, CancellationToken ct);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="container">Where the file is.</param>
    /// <param name="id">The file ID.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task DeleteAsync(string container, string id, CancellationToken ct);

    /// <summary>
    /// Checks to see if the file exists.
    /// </summary>
    /// <param name="container">Where the file would be.</param>
    /// <param name="id">The file ID.</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> Exists(string container, string id, CancellationToken ct);
}
