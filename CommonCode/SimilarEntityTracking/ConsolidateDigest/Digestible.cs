using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using Newtonsoft.Json;

namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

/// <summary>
/// Wraps the given type in an implementation of IDigestible,
/// making it ready for use by the consolidation service.
/// </summary>
/// <typeparam name="T">Type to wrap.</typeparam>
public class Digestible<T> : IDigestible
    where T : class, new()
{
    public Digestible()
    {

    }


    /// <summary>
    /// Creates an instance ready to be consumed by the 
    /// consolidation service.
    /// </summary>
    /// <param name="item">Item to consolidate.</param>
    /// <param name="until">Time finish and publish the digest; only the first item in the digest will be respected.</param>
    /// <param name="headLimit">Total items allowed in the head of the digest, first come first stored. Only the first item in the digest will be respected.</param>
    /// <param name="tailLimit">Total items allowed in the tail of the digest, representing the last items added. Only the first item in the digest will control this parameter.</param>
    /// <param name="itemId">An id or key common to all of the items to be consolidated.</param>
    /// <param name="forwardToExchange">Exchange to send the digest to, defined by the first item digested. If provided as the default string.Empty, a default exchange will be used.</param>
    /// <param name="forwardToRoute">Route to send the digest to, defined by the first item digested. If provided as the default string.Empty, a default route will be used.</param>
    /// <param name="props">A set of functions that provide string representations of the item's properties. These properties are used to determine if items are similar enough to digest together, if these properties are equal.</param>
    public Digestible(T item, 
        DateTime until, 
        int headLimit, int tailLimit,
        string itemId,
        params Func<T,string>[] props)
    {
        item.Requires().IsNotNull();
        headLimit.Requires().IsGreaterOrEqual(1);
        tailLimit.Requires().IsGreaterOrEqual(1);
        itemId.Requires().IsNotNullOrEmpty();
        props.Requires().IsNotEmpty();

       
        DigestUntil = until;
        InvocationTime = DateTime.UtcNow;
        DigestBodyJson = JsonConvert.SerializeObject(item, Formatting.Indented);
        HeadLimit = headLimit;
        TailLimit = tailLimit;
   
        TargetId = itemId;
        ComparisonType = typeof(T).GetFriendlyTypeName();
        ComparisonPropertyString = String.Join(',', props.Select(fn => fn(item)));
        ComparisonHash = ComparisonPropertyString.GetHashCode();
    }


    public DateTime DigestUntil { get; set; }
    public DateTime InvocationTime { get; set; }
    public string DigestBodyJson { get; set; } = "";
    public int HeadLimit { get; set; }
    public int TailLimit { get; set; }

    public string ForwardToExchange { get; set; } = "";
    public string ForwardToRoute { get; set; } = "";
    public string TargetId { get; set; } = "";

    public string ComparisonType { get; set; } = "";

    public long ComparisonHash { get; set; }

    public string ComparisonPropertyString { get; set; } = "";
}
