using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json;
using System.Text;

namespace BFormDomain.CommonCode.Utility;

public static class BsonDocumentExtensions
{
    public static string ToJsonString(this BsonDocument bson)
    {
        using var stream = new MemoryStream();
        using (var writer = new BsonBinaryWriter(stream))
        {
            BsonSerializer.Serialize(writer, typeof(BsonDocument), bson);
        }

        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new Newtonsoft.Json.Bson.BsonDataReader(stream);

        var sb = new StringBuilder();
        var sw = new StringWriter(sb);

        using (var jWriter = new JsonTextWriter(sw))
        {
            jWriter.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            jWriter.WriteToken(reader);
        }
        return sb.ToString();
    }
}
