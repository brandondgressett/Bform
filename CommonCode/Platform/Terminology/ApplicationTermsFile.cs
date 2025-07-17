using BFormDomain.CommonCode.Platform.Terminology;
using BFormDomain.Diagnostics;
using BFormDomain.HelperClasses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform;

/// <summary>
/// Bind as singleton object
/// FileApplicationTerms implements ReplaceTerms from IApplicationTerms to allow client to replace terms with new ones via source string
///     -References:
///         >InvitationLogic.cs
///         >UserManagementLogic.cs
///         >UIExceptionFunnel.cs
///         >UIExceptionFunnelRegistery.cs
///         >FormLogic.cs
///         >FormTemplateViewModel.cs
///         >KPIEvaluator.cs
///         >KPILogic.cs
///         >KPIViewModel.cs
///         >ReportLogic.cs
///         >FindReplaceAppender.cs
///         >ApplicationTermsFile.cs
///         >WorkItemLogic.cs
///         >WorkSetLogic.cs
///     -Functions:
///         >MaybeInitialize
///         >Replace
///         >ReplaceTerms
/// </summary>
public class FileApplicationTerms: IApplicationTerms
{
    
    private readonly string _metadataFilePath;
    private readonly string _metadataDirPath;
    private const string TerminologyFile = "AppTerms.json";
    private readonly ConcurrentDictionary<string, string> _terms = new();
    private readonly object _door = new();
    private readonly IApplicationAlert _alerts;

    private const string FileRef = "ref-file:";

    public FileApplicationTerms(IOptions<FileApplicationTermsOptions> options, IApplicationAlert alerts)
    {

        _metadataDirPath = options.Value.FilePath;
        if (string.IsNullOrWhiteSpace(_metadataDirPath))
            _metadataDirPath = Path.Join(Environment.CurrentDirectory, "Content", "Terminology");
        _metadataFilePath = Path.Join(_metadataDirPath, TerminologyFile);
        _alerts = alerts;
    }

    private void MaybeInitialize()
    {
        lock(_door)
        {
            if(!_terms.Any())
            {

                if (File.Exists(_metadataFilePath))
                {

                    var text = File.ReadAllText(_metadataFilePath);
                    
                    Dictionary<string, string>? terms = null!;
                    try
                    {
                        terms = JsonConvert.DeserializeObject<Dictionary<string, string>>(text!);
                    } catch(Exception ex)
                    {
                        var problem = $"Terminology file '{_metadataFilePath}' could not be deserialized.";
                        _alerts.RaiseAlert(ApplicationAlertKind.InputOutput, LogLevel.Critical, 
                            $"{problem}: {ex.TraceInformation()}");
                    }

                    if (terms is not null)
                    {

                        foreach (var kvp in terms!)
                        {
                            var value = kvp.Value;
                            if (value.StartsWith(FileRef))
                            {
                                string fileName = Path.Join(
                                    _metadataDirPath,
                                    value[FileRef.Length..]);

                                if (File.Exists(fileName))
                                {
                                    value = File.ReadAllText(fileName);
                                    
                                }
                                else
                                {
                                    var problem = $"Terminology file '{fileName}' does not exist.";
                                    _alerts.RaiseAlert(ApplicationAlertKind.InputOutput, LogLevel.Critical, problem);
                                }

                            }

                            _terms[kvp.Key] = value;
                        }
                    } else
                    {
                        var problem = $"Terminology file '{_metadataFilePath}' could not be deserialized.";
                        _alerts.RaiseAlert(ApplicationAlertKind.InputOutput, LogLevel.Critical, problem);
                    }

                }
                else
                {
                    var problem = $"Terminology file '{_metadataFilePath}' does not exist.";
                    _alerts.RaiseAlert(ApplicationAlertKind.InputOutput, LogLevel.Critical, problem);
                }

                var defaults = DefaultTerminology.Terms;
                foreach (var kvp in defaults)
                    if(!_terms.ContainsKey(kvp.Key))
                        _terms[kvp.Key] = kvp.Value;
            }
        }
    }

    public IReadOnlyDictionary<string,string> ApplicationTerms
    {
        get
        {
            MaybeInitialize();

            return _terms;
        }
    }

    private static string Replace(string source, IDictionary<string, string> values)
    {
        return values.Aggregate(
            source,
            (current, parameter) => current
                .Replace($"{{{parameter.Key}}}", parameter.Value));
    }

    public string ReplaceTerms(string source)
    {
        MaybeInitialize();
        return Replace(source, _terms);
    }

}
