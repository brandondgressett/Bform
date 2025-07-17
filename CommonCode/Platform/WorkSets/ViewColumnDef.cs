using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.WorkSets;



public abstract class ViewColumnDef
{
    public abstract string Kind { get; }
}


public class ViewNestedGridDef: ViewColumnDef
{
    public override string Kind => nameof(ViewNestedGridDef);

    [JsonProperty(ItemConverterType = typeof(ViewRowDefConverter))]
    public List<ViewRowDef> NestedGrid { get; set; } = new();
}


public class ViewPerColumnDef: ViewColumnDef
{

    public override string Kind => nameof(ViewPerColumnDef);


    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]

    public List<GridColumnSizes> Sizes { get; set; } = new();
    
    public ViewDataQuery ColumnQuery { get; set; } = null!;

    public string Renderer { get; set; } = null!;

    

}

public class ViewColumnDefConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(ViewColumnDef));
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        if (jo["Kind"]!.Value<string>() == nameof(ViewSeveralRowDef))
            return jo.ToObject<ViewSeveralRowDef>(serializer!)!;

        if (jo["Kind"]!.Value<string>() == nameof(ViewPerColumnDef))
            return jo.ToObject<ViewPerColumnDef>(serializer!)!;

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