using BFormDomain.CommonCode.Platform.Entity;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BFormDomain.CommonCode.Platform.Content;

/// <summary>
/// Accesses content entities from files stored in the configured location.
/// Call Initialize() to set up access.
/// </summary>
public class FileApplicationPlatformContent: IApplicationPlatformContent
{
    protected class SchemaResource
    {
        public Assembly? Assembly { get; set; }
        public string[]? SchemaNames { get; set; }
    }

    protected class ContentInfo
    {
        public DateTime Initialized { get; set; }
        public const string FileName = "BFormContentInitialized.json";
    }

    protected readonly ConcurrentBag<ContentDomain> _domains = new();
    protected readonly ConcurrentBag<ContentElement> _elements = new();
    protected readonly ConcurrentBag<SchemaResource> _schemaResources = new();
    protected readonly ConcurrentDictionary<string, string> _schemaResourcesDic = new();
    protected readonly ConcurrentDictionary<string, string> _freeJson = new();
    protected readonly ILogger<FileApplicationPlatformContent> _logger;
    protected readonly IApplicationAlert _alerts;
    protected readonly IEnumerable<IContentDomainSource> _contentDomainSources;
    protected readonly IEnumerable<IEntityInstanceLogic> _instanceConsumers;
    protected readonly string _baseFolder, _schemaFolder;
    protected bool _initialized = false;
    protected readonly object _door = new();
       
    public FileApplicationPlatformContent(
        ILogger<FileApplicationPlatformContent> logger,
        IApplicationAlert alert,
        IEnumerable<IContentDomainSource> contentDomainSources,
        IEnumerable<IEntityInstanceLogic> instanceConsumers,
        IOptions<FileApplicationPlatformContentOptions> options)
    {
        _logger = logger;
        _alerts = alert;
        _contentDomainSources = contentDomainSources;
        _instanceConsumers = instanceConsumers;
        var optionsVal = options.Value;
        _baseFolder = optionsVal.BaseFolder;
        _schemaFolder = optionsVal.SchemaFolder;
               

        if (string.IsNullOrWhiteSpace(_baseFolder))
            _baseFolder = Environment.CurrentDirectory;

        if(string.IsNullOrWhiteSpace(_schemaFolder))
            _schemaFolder = Environment.CurrentDirectory;

        Initialize();
    }

    static string Clean(string script)

    {
        var blockComments = @"/*(.?)*/";
        var lineComments = @"//(.?)\r?\n";
        var strings = @"""((\[^\n]|[^""\n]))""";
        var verbatimStrings = @"@(""[^""]"")+";

        string cleaned = Regex.Replace(script,
                    blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                    me =>
                    {
                        if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                            return me.Value.StartsWith("//") ? Environment.NewLine : "";
                    // Keep the literal strings
                    return me.Value;
                    },
                    RegexOptions.Singleline);

        return cleaned;
    }

    protected string FindSchema(string name)
    {
        
        name.Requires().IsNotNullOrEmpty();

        var matching = _schemaResourcesDic.FirstOrDefault(it => it.Key.Contains(name));
        
        matching.Guarantees($"Cannot find embedded schema for content domain {name}.");

        return matching.Value;
    }

    protected JSchema LoadEmbeddedSchema(string name)
    {
      
        name.Requires().IsNotNullOrEmpty();
        
        var schemaText = Clean(File.ReadAllText(FindSchema(name)));
        
        return JSchema.Parse(schemaText);
    }

    public JSchema LoadEmbeddedSchema<T>() where T : IContentType
    {
        var name = $"BForm-Schema-{typeof(T).GetFriendlyTypeName()}";
        return LoadEmbeddedSchema(name);
    }

    protected virtual void RegisterDomains()
    {
        foreach(var cd in _contentDomainSources)
        {
            _domains.Add(cd.Tell(this));
        }
    }

    protected void LoadEmbeddedSchemas()
    {
        var cd = Environment.CurrentDirectory;
        var startFolder = Path.Combine(cd, _schemaFolder);
        var matchingFiles = Directory.EnumerateFiles(startFolder, "*.json", SearchOption.AllDirectories);
        foreach (string filePath in matchingFiles)
        {
            var fileName=  Path.GetFileName(filePath);
            
            _schemaResourcesDic[fileName] = filePath;
        }     
    }

    protected void Initialize()
    {
        if (_initialized)
            return;

        lock(_door)
        {
            if(!_initialized)
            {
                
                var initFileName = Path.Join(_baseFolder, ContentInfo.FileName);
                bool contentIsInitializedMarker = File.Exists(initFileName);


                LoadEmbeddedSchemas();
                RegisterDomains();
                LoadContentElements(contentIsInitializedMarker);

                File.WriteAllText(initFileName, JsonConvert.SerializeObject(new ContentInfo { Initialized = DateTime.UtcNow }));
            }

            _initialized = true;
        }

        void AddContentElement(ContentDomain matchingDomain, string text, JObject json, string filePath)
        {
            var jtName = json.GetValue("name");
            jtName.Guarantees().IsNotNull();
            string name = (string)jtName!;
            var jtOrder = json.GetValue("descendingOrder");
            jtOrder.Guarantees().IsNotNull();
            int descOrder = (int)jtOrder!;

            var jtTags = json.GetValue("tags");
            jtTags.Guarantees().IsNotNull();
            JArray jtTagsArray = (JArray)jtTags!;
            var tagsList = jtTagsArray.Select(jt => (string)jt!).ToList();

            var @object = json.ToObject(matchingDomain.ContentType!)!;
            @object.Guarantees().IsNotNull();
            var ct = (IContentType) @object;

            var copy = ct.SatelliteData!.ToArray();   
            ct.SatelliteData!.Clear();
                        
            if (copy.Any())
            {
                var mainDir = Path.GetDirectoryName(filePath); // satellites are in same dir.
                var fn = Path.GetFileNameWithoutExtension(filePath);

                foreach (var satellite in copy)
                {
                    // satellite data is Name:FileName
          
                    var findPath = Path.Join(mainDir, $"{satellite.Value}.json");
                    if (File.Exists(findPath))
                    {
                        var satText = Clean(File.ReadAllText(findPath));

                        // TODO check for schema to validate?


                        ct.SatelliteData[satellite.Key.ToLowerInvariant()] = satText;
                    }
                    else
                    {
                        _alerts.RaiseAlert(ApplicationAlertKind.InputOutput,
                            LogLevel.Error,
                            $"Cannot load satellite json. Main file:{filePath}. Satellite:{findPath}");
                    }
                }

            }
           
            _elements.Add(new ContentElement
            {
                Name = name,
                DescendingOrder = descOrder,
                Serialized = text,
                Json = json,
                DomainName = matchingDomain!.Name,
                Tags = tagsList,
                Deserialized = @object
            });
        }

        void LoadContentElements(bool contentIsInitializedMarker)
        {
            var entityInstances = new List<(ContentDomain, string)>();
            var startingFolder = Path.Combine(Environment.CurrentDirectory, _baseFolder);
            var matchingFiles = Directory.EnumerateFiles(startingFolder, "*.json", SearchOption.AllDirectories)
                                .ToList();


            foreach (string filePath in matchingFiles)
            {
                try
                {
                    if (filePath.Contains(ContentInfo.FileName))
                        continue;

                    if(filePath.EndsWith("free.json"))
                    {
                        var text = Clean(File.ReadAllText(filePath));
                        var name = Path.GetFileNameWithoutExtension(filePath);
                        _freeJson[name] = text;
                        continue;
                    }

                    var matchingDomain = _domains
                        .SingleOrDefault(
                            domain => filePath.EndsWith(domain.Extension));

                    var matchingInstanceDomain = _domains
                            .SingleOrDefault(
                                domain => filePath.EndsWith(domain.InstanceExtension));

                    if (matchingDomain is not null)
                    {
                        var text = Clean(File.ReadAllText(filePath));
                        var json = JObject.Parse(text);
                        var schema = matchingDomain.Schema;
                        bool isValid = json.IsValid(schema!);

                        if (!isValid)
                        {
                            _alerts.RaiseAlert(ApplicationAlertKind.Defect, LogLevel.Critical, $"content in {filePath} doesn't conform to entity schema.");
                            continue;
                        }

                        AddContentElement(matchingDomain, text, json, filePath);

                    }
                    else if (matchingInstanceDomain is not null)
                    {
                        var text = Clean(File.ReadAllText(filePath));
                        entityInstances.Add((matchingInstanceDomain, text));

                        continue;
                    }
                    else
                    {
                        if(!filePath.EndsWith(".s.json")) // we can ignore satellite json files
                            _alerts.RaiseAlert(ApplicationAlertKind.System, LogLevel.Warning, $"Ignoring file {filePath} as it doesn't match a domain.");
                    }


                }
                catch (Exception ex)
                {
                    _alerts.RaiseAlert(ApplicationAlertKind.Defect, LogLevel.Critical, $"Problem loading {filePath} into content: {ex.Message}");
                }
            }

            // respect instance creation order.
            var ordered = entityInstances.OrderByDescending(it => it.Item1.InstanceGroupDescOrder);
            foreach(var instance in ordered)
            {
                var (matchingInstanceDomain, text) = instance;
                var handler = _instanceConsumers.First(it => it.Domain == matchingInstanceDomain.Name);
                AsyncHelper.RunSync(()=>handler.AcceptContentInstance(text, contentIsInitializedMarker));
            }




        }

        
    }

    public IEnumerable<ContentDomain> Domains { get { return _domains.OrderBy(it => it.Name); } }

    public IList<T> GetAllContent<T>() where T : IContentType
    {
        Initialize();

        var matches = _elements
            .Where(it => it.DomainName == typeof(T).GetFriendlyTypeName())
            .OrderByDescending(it => it.DescendingOrder)
            .Select(it => (T)it.Deserialized!)
            .ToList();

        return matches;
    }

    public T? GetContentByName<T>(string name) where T : IContentType
    {
        name.Requires().IsNotNullOrEmpty();

        Initialize();

        var match = _elements
            .Where(it => it.DomainName == typeof(T).GetFriendlyTypeName() &&
                         it.Name == name)
            .OrderByDescending(it => it.DescendingOrder)
            .Select(it => (T)it.Deserialized!)
            .FirstOrDefault();

        return match;
    }

    public ContentElement? ViewContentType(string name)
    {
        name.Requires().IsNotNullOrEmpty();

        Initialize();

        var match = _elements
            .Where(it => it.Name == name)
            .OrderByDescending(it => it.DescendingOrder)
            
            .FirstOrDefault();

        return match;
    }

    public IList<T> GetMatchingAny<T>(params string[] tags) where T : IContentType
    {
        tags.Requires().IsNotEmpty();

        Initialize();

        var matches = _elements
            .Where(it => it.DomainName == typeof(T).GetFriendlyTypeName() && 
                         it.Tags.Any(tg=>tags.Contains(tg)))
            .OrderByDescending(it => it.DescendingOrder)
            .Select(it => (T) it.Deserialized!)
            .ToList();

        return matches;
    }

    public IList<T> GetMatchingAll<T>(params string[] tags) where T : IContentType
    {
        tags.Requires().IsNotEmpty();

        Initialize();

        var matches = _elements
            .Where(it => it.DomainName == typeof(T).GetFriendlyTypeName() &&
                         it.Tags.All(tg => tags.Contains(tg)))
            .OrderByDescending(it => it.DescendingOrder)
            .Select(it => (T)it.Deserialized!)
            .ToList();

        return matches;
    }

    public string? GetFreeJson(string name)
    {
        if(_freeJson.ContainsKey(name))
            return _freeJson[name]; 
        return null;
    }
}
