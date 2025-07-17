using BFormDomain.DataModels;
using MongoDB.Bson.Serialization.Attributes;

namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// 
/// </summary>
public class SuppressedItem : IWillShutUp, IDataModel
{
       
    /// <summary>
    /// 
    /// </summary>
    public string TargetId { get; set; } = "none";
        
    /// <summary>
    /// 
    /// </summary>
    public string ComparisonType { get; set; } = "none";

    /// <summary>
    /// 
    /// </summary>
    public long ComparisonHash { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string ComparisonPropertyString { get; set; } = "";

    /// <summary>
    /// 
    /// </summary>
    public int SuppressionTimeMinutes { get; set; } = 8 * 60;

    /// <summary>
    /// 
    /// </summary>
    public DateTime SuppressionStartTime { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string ItemId
    {
        get { return $" {TargetId} {ComparisonHash}"; }
        set { }
    }

    /// <summary>
    /// 
    /// </summary>
    public int Version { get; set; }


    /// <summary>
    /// 
    /// </summary>
    [BsonId]
    public Guid Id { get; set; }
}

