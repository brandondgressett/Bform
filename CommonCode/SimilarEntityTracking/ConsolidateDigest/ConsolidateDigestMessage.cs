namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

/// <summary>
/// Represents a message, presented to the message bus, to be consumed by the
/// consolidated digest service and process the given item (as represented in
/// json) into a digest of items over time.
/// </summary>
public class ConsolidateDigestMessage : IDigestible
{
    public ConsolidateDigestMessage()
    {

    }

    public ConsolidateDigestMessage(IDigestible digestible)
    {
        DigestUntil = digestible.DigestUntil;
        InvocationTime = digestible.InvocationTime;
        DigestBodyJson = digestible.DigestBodyJson;
        HeadLimit = digestible.HeadLimit;
        TailLimit = digestible.TailLimit;
        ForwardToExchange = digestible.ForwardToExchange;
        ForwardToRoute = digestible.ForwardToRoute;
        TargetId = digestible.TargetId;
        ComparisonType = digestible.ComparisonType;
        ComparisonHash = digestible.ComparisonHash;
        ComparisonPropertyString = digestible.ComparisonPropertyString;
    }

    public DateTime DigestUntil { get; set; }
    public DateTime InvocationTime { get; set; }
    public string DigestBodyJson { get; set; } = String.Empty;
    public int HeadLimit { get; set; }
    public int TailLimit { get; set; }
    public string ForwardToExchange { get; set; } = String.Empty;
    public string ForwardToRoute { get; set; } = String.Empty;
    public string TargetId { get; set; } = String.Empty;

    public string ComparisonType { get; set; } = String.Empty;

    public long ComparisonHash { get; set; }

    public string ComparisonPropertyString { get; set; } = String.Empty;
}
