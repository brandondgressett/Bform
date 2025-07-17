using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace BFormDomain.CommonCode.Utility;

public class JsonFromSchema
{
    public JsonFromSchema()
    {
    }

    public static JToken? Generate(JSchema schema)
    {
        JToken? output;
        

        switch (schema.Type)
        {
            case JSchemaType.Object:
                var jObject = new JObject();
                if (schema.Properties is not null)
                {
                    foreach (var prop in schema.Properties)
                    {
                        jObject.Add(prop.Key, Generate(prop.Value));
                    }
                }
                output = jObject;
                break;

            case JSchemaType.Array:
                var jArray = new JArray();
                foreach (var item in schema.Items)
                {
                    var prop = Generate(item);
                    if(prop is not null)
                        jArray.Add(prop);
                }
                output = jArray;
                break;

            case JSchemaType.String:
                output = new JValue((string)schema.Default!);
                break;

            case JSchemaType.Number:
                output = new JValue((float)schema.Default!);
                break;

            case JSchemaType.Integer:
                output = new JValue((int)schema.Default!);
                break;

            case JSchemaType.Boolean:
                output = new JValue((bool)schema.Default!);
                break;

            case JSchemaType.Null:
                output = JValue.CreateNull();
                break;

            default:
                output = null!;
                break;

        }


        return output;
    }

    
}

public static class JsonUtility
{
    public static BsonDocument ToBsonObject(this JObject that)
    {
        var json = that.ToString();
        return BsonDocument.Parse(json);
    }


}


public class BsonToJsonConverter: JsonConverter
{
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var bytes = reader.ReadAsBytes();
        return new RawBsonDocument(bytes);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not null)
        {
            var doc = (BsonDocument)value;
            var bytes = doc.ToBson();
            writer.WriteValue(bytes);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(BsonDocument);
    }
}
