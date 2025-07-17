namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

public class DigestItem<T>
    where T : class, new()
{
    public DateTime DateTime { get; set; }
    public T? Item { get; set; }
}
