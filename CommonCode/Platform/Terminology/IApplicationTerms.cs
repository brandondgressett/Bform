namespace BFormDomain.CommonCode.Platform;

public interface IApplicationTerms
{
    IReadOnlyDictionary<string, string> ApplicationTerms { get;  }
    string ReplaceTerms(string source);
}
