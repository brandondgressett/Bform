using BFormDomain.CommonCode.Platform.Tags;
using BFormDomain.CommonCode.Utility;
using BFormDomain.DataModels;
using BFormDomain.Validation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tables;

public class TableRowData : IDataModel, ITaggable
{
    [BsonId]
    public Guid Id { get; set; }
    public int Version { get; set; }

    public string? KeyRowId { get; set; }
    public DateTime? KeyDate { get; set; }
    public Guid? KeyUser { get; set; }
    public Guid? KeyWorkSet { get; set; }
    public Guid? KeyWorkItem { get; set; }

    public double? KeyNumeric { get; set; }
    public List<string> Tags { get; set; } = new();

    [JsonConverter(typeof(BsonToJsonConverter))]
    public BsonDocument? PropertyBag { get; set; }

    public DateTime Created { get; set; }
    
    public void SetProperties(JObject obj, TableTemplate guide)
    {
        
        PropertyBag = obj.ToBsonObject();

        foreach(var projection in guide.Columns)
        {
            if (projection.KeyType == KeyType.NotKey)
                continue;

            var jt = obj.SelectToken(projection.Field)!;
            jt.Guarantees().IsNotNull();

            switch (projection.KeyType)
            {
                case KeyType.KeyDate:
                    KeyDate = jt.Value<DateTime>();
                    break;

                case KeyType.KeyUser:
                    KeyUser = jt.Value<Guid>();
                    break;

                case KeyType.KeyRowId:
                    KeyRowId = jt.Value<string>();
                    break;

                case KeyType.KeyWorkSet:
                    KeyWorkSet = jt.Value<Guid>();
                    break;

                case KeyType.KeyWorkItem:
                    KeyWorkItem = jt.Value<Guid>();
                    break;

                case KeyType.KeyNumeric:
                    KeyNumeric = jt.Value<double>();
                    break;

                case KeyType.KeyTags:
                    var vals = jt.Values<string>();
                    if(vals is not null && vals.Any())
                        Tags = TagUtil.MakeTags(vals!).ToList();
                    break;
              

                default:
                    break;

            }
        }
    }

}
