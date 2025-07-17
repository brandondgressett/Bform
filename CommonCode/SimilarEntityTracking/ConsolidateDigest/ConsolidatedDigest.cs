using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

/// <summary>
/// Data model representing a digest of consolidated items,
/// used for persistence of digests into storage.
/// </summary>
public class ConsolidatedDigest : IConsolidatedDigest, IDataModel
{

    [BsonId]
    public Guid Id {get;set;}
    public DateTime DigestUntil { get;set;}
    public int HeadLimit { get; set; } = 100;
    public int TailLimit { get; set; } = 10;
    public bool Complete { get; set; } = false;

    public DigestModel Head { get; set; } = new DigestModel();
    public DigestModel Tail { get; set; } = new DigestModel();    

    [BsonIgnore]
    public DigestModel CurrentDigest
    {
        get
        {
            return DigestModel.Merge(Head, Tail);
        }
    }

    public string ForwardToExchange { get; set; } = String.Empty;
    public string ForwardToRoute { get; set; } = String.Empty;
    public string TargetId { get; set; } = String.Empty;

    public string ComparisonType { get; set; } = String.Empty;

    public long ComparisonHash { get; set; }

    public string ComparisonPropertyString {get; set;} = String.Empty;  

    public int Version { get; set; }
}
