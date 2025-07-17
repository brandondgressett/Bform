using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPISignalViewModel
{
    public double Value { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public KPISignalType SignalType { get; set; }

    public DateTime SignalTime { get; set; }
}
