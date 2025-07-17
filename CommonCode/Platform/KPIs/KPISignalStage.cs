using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BFormDomain.CommonCode.Platform.KPIs;

public class KPISignalStage
{
    public string SignalName { get; set; } = null!;
    public string Title { get; set; } = null!;

    

    public int SignalId { get; set; }

    public List<string> ScriptLines { get; set; } = new();
}
