namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

public interface IDigestible: ITrackSimilar
{
    public DateTime DigestUntil { get; set; }
    public DateTime InvocationTime { get; set; }
    public string DigestBodyJson { get; set; }

    public int HeadLimit { get; set; }
    public int TailLimit { get; set; }

    public string ForwardToExchange { get; set; }
    public string ForwardToRoute { get; set; }

}
