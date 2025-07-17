using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Options;

namespace BFormDomain.CommonCode.Platform.ManagedFiles;
/// <summary>
/// The actual storage of the file.
/// 
/// "Actual" is so VAGUE. Get Specific. Get detailed.
/// This implementation of the IManagedFilePersistence interface
/// is usable by the ManangedFileLogic to store and retrieve file contents.
/// It stores them in the file system in the folder provided by the configuration
/// options.
/// 
///  
/// 
/// </summary>
public class PhysicalFilePersistence: IManagedFilePersistence
{
    /// <summary>
    /// Where the file is stored.
    /// </summary>
    private readonly string _basePath;
    /// <summary>
    /// The alarm for Physical File Persistence.
    /// 
    /// "ALARM". It's wrong, it's totally vague, it is worse than no comment because it can only CONFUSE.
    /// This is the operational alerts service, to record errors and exceptions of note.
    /// </summary>
    private readonly IApplicationAlert _alerts;
    /// <summary>
    /// How many errors before we do something.
    /// 
    /// Do what? Errors while doing WHAT? So vague. 
    /// Controls alert notification threshold on the operational alerts service.
    /// </summary>
    private readonly int _errThreshold;
    /// <summary>
    /// The largest a file can be.
    /// 
    /// VAGUE. In Gigabytes? Ever in the universe? Total sum of all files? 
    /// 
    /// </summary>
    private readonly long _maxFileSize;

    public PhysicalFilePersistence(
        IOptions<PhysicalFilePersistenceOptions> options,
        IApplicationAlert alerts)
    {
        var optVal = options.Value;
        _basePath = optVal.BasePath;
        _errThreshold = optVal.ErrorThreshold;
        _maxFileSize = optVal.MaximumBytes;
        _alerts = alerts;
    }

    /// <summary>
    /// Gets the path to the file based on ID.
    /// </summary>
    /// <param name="container">Where the file is stored.</param>
    /// <param name="id">The ID of the file.</param>
    /// <returns>The path to the file.</returns>
    private string GetFilePath(string container, string id)
    {
        var fileName = id + ".bin";
        return Path.Join(_basePath, container, fileName);
    }

    /// <summary>
    /// EMPTY -- SHOULD THERE BE A QUESTION ABOUT THIS?
    /// </summary>
    /// <param name="container"></param>
    private void AssureContainer(string container)
    {
        var dirPath = Path.Join(_basePath, container);
        if(!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
    }
    /// <summary>
    /// Retrieves a file. 
    /// 
    /// 
    /// TOO VAGUE. IT RETRIEVES THE CONTENTS OF A FILE.
    /// </summary>
    /// <param name="container">What the file is stored in.</param>
    /// <param name="id">The file ID</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public Task<Stream> RetrieveAsync(string container, string id, CancellationToken ct)
    {
        try
        {
            container.Requires().IsNotNullOrEmpty();
            container.Requires().DoesNotContainAny(EnumerableEx.OfTwo('\\', '/')); // no subfolders! managed files can be organized by tag.
            id.Requires().IsNotNullOrEmpty();
            AssureContainer(container);
            var filePath = GetFilePath(container, id);
            ct.ThrowIfCancellationRequested();

            if (File.Exists(filePath))
            {
                return Task.FromResult((Stream) new FileStream(filePath, FileMode.Open, FileAccess.Read));
            }
            else
                throw new FileNotFoundException($"File peristence could not find file {id} in container {container}.");

        } catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services, Microsoft.Extensions.Logging.LogLevel.Information,
                ex.Message, _errThreshold, nameof(PhysicalFilePersistence));
            throw;
        }
    }
    /// <summary>
    /// Update a File.
    /// 
    /// Updates the contents of a file
    /// </summary>
    /// <param name="container">What the file is stored in.</param> NOT AT ALL ACCURATE. CONTAINER IS A FOLDER, NOT A FILE.
    /// <param name="id">The file ID</param>
    /// <param name="stream">The updated data</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task UpsertAsync(string container, string id, Stream stream, CancellationToken ct)
    {
        try
        {
            container.Requires().IsNotNullOrEmpty();
            container.Requires().DoesNotContainAny(EnumerableEx.OfTwo('\\','/')); // no subfolders! managed files can be organized by tag.
            id.Requires().IsNotNullOrEmpty();
            stream.Requires().IsNotNull();
            stream.Length.Requires("File too large").IsLessOrEqual(_maxFileSize);
            
            AssureContainer(container);
            var filePath = GetFilePath(container, id);

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await fs.CopyToAsync(stream, ct);

        } 
        catch(Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services, Microsoft.Extensions.Logging.LogLevel.Information,
                ex.Message, _errThreshold, nameof(PhysicalFilePersistence));
            throw;
        }
    }
    /// <summary>
    /// Delete A file
    /// </summary>
    /// <param name="container">What the file is stored in.</param>
    /// <param name="id">The File ID</param>
    /// <param name="ct"></param>
    /// <returns>Returns if the task was completed or not.</returns>
    public Task DeleteAsync(string container, string id, CancellationToken ct)
    {
        try
        {
            container.Requires().IsNotNullOrEmpty();
            container.Requires().DoesNotContainAny(EnumerableEx.OfTwo('\\', '/')); // no subfolders! managed files can be organized by tag.
            id.Requires().IsNotNullOrEmpty();
            AssureContainer(container);
            var filePath = GetFilePath(container, id);
            ct.ThrowIfCancellationRequested();

            if (File.Exists(filePath))
                File.Delete(filePath);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services, Microsoft.Extensions.Logging.LogLevel.Information,
                ex.Message, _errThreshold, nameof(PhysicalFilePersistence));
            throw;
        }


    }
    /// <summary>
    /// Checks to see if a file exists
    /// </summary>
    /// <param name="container">What the file is stored in.</param>
    /// <param name="id">File ID</param>
    /// <param name="ct"></param>
    /// <returns>Returns if the task was completed or not.</returns>
    public Task<bool> Exists(string container, string id, CancellationToken ct)
    {
        try
        {
            container.Requires().IsNotNullOrEmpty();
            container.Requires().DoesNotContainAny(EnumerableEx.OfTwo('\\', '/')); // no subfolders! managed files can be organized by tag.
            id.Requires().IsNotNullOrEmpty();
            AssureContainer(container);
            var filePath = GetFilePath(container, id);
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(File.Exists(filePath));
        }
        catch (Exception ex)
        {
            _alerts.RaiseAlert(ApplicationAlertKind.Services, Microsoft.Extensions.Logging.LogLevel.Information,
                ex.Message, _errThreshold, nameof(PhysicalFilePersistence));
            throw;
        }
    }
}
