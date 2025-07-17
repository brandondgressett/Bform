using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkSets;

public abstract class ViewRowDef
{
    public abstract string Kind { get; }
}

public class ViewSeveralRowDef: ViewRowDef
{
    public override string Kind => nameof(ViewSeveralRowDef);

    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
    public List<GridColumnSizes> PrototypeColumnSizes { get; set; } = new();

    public ViewDataQuery RowQuery { get; set; } = null!;

    public string Renderer { get; set; } = null!;
}

public class ViewColumnsRowDef: ViewRowDef
{

    public override string Kind => nameof(ViewColumnsRowDef);

    /// <summary>
    /// Defines size and contents of columns.
    /// </summary>
    [JsonRequired]
    [JsonProperty(ItemConverterType = typeof(ViewColumnDefConverter))]
    public List<ViewColumnDef> Columns { get; set; } = new();

}


public class ViewRowDefConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(ViewRowDef));
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        if (jo["Kind"]!.Value<string>() == nameof(ViewSeveralRowDef))
            return jo.ToObject<ViewSeveralRowDef>(serializer!)!;

        if (jo["Kind"]!.Value<string>() == nameof(ViewColumnsRowDef))
            return jo.ToObject<ViewColumnsRowDef>(serializer!)!;

        return null;
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}
