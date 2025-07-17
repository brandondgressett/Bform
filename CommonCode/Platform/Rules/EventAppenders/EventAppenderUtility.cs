using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Rules.EventAppenders;

public static class EventAppenderUtility
{


    public static string FixName(string className)
    {
        className.Requires().IsNotNullOrEmpty();
        return className.Replace("Appender", string.Empty);
    }

    

}
