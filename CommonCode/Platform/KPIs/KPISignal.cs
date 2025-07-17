using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BFormDomain.CommonCode.Platform.KPIs;



public class KPISignal
{
    public string SignalName { get; set; } = null!;
    public string? Title { get; set; }

    [BsonRepresentation(BsonType.String)]
    [JsonConverter(typeof(StringEnumConverter))]
    public KPISignalType SignalType { get; set; }

    public DateTime SignalTime { get; set; }

    public int SignalId { get; set; }
    public double SignalValue { get; set; }

}