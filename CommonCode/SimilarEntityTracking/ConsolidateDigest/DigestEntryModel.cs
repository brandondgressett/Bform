

using BFormDomain.CommonCode.Utility;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

/// <summary>
/// Data model to contain a digest entry in persistent storage.
/// </summary>
public class DigestEntryModel
{
    public DateTime InvocationTime { get; set; }

    // the extra dependency on the mongodb c# driver here is ... unfortunate,
    // but the simplest way to proceed.
    [JsonConverter(typeof(BsonToJsonConverter))]
    public BsonDocument Entry { get; set; } = null!;
}
