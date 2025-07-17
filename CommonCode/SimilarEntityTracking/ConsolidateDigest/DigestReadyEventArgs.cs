namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

public class DigestReadyEventArgs<T>: EventArgs
    where T: class, new()
{
    public List<DigestItem<T>> Digest { get; set; } = new List<DigestItem<T>>();
}
