namespace BFormDomain.CommonCode.Logic;

public interface ITrackSimilar
{
    public string TargetId { get; set; }
    public string ComparisonType { get; }
    public long ComparisonHash { get; }
    public string ComparisonPropertyString { get; }
}
