namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

public interface IConsolidatedDigest : ITrackSimilar
{
    public Guid Id { get; set; }
    public DateTime DigestUntil { get; set; }

    public int HeadLimit { get; set; }
    public int TailLimit { get; set; }

    public bool Complete { get; set; }

    public DigestModel CurrentDigest { get; }

    public string ForwardToExchange { get; set; }
    public string ForwardToRoute { get; set; }
}
