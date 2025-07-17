namespace BFormDomain.CommonCode.Utility.Caching;

public enum ExpirationReason
{
    LifeTimeExceeded,
    RemoveOldestForSpace,
};

public class ExpiredEventArgs<KeyType, DataType> : EventArgs
{
    private readonly DataType? _data;
    private readonly KeyType _key;
    private readonly ExpirationReason _reason;

    public ExpiredEventArgs(KeyType key, DataType? data, ExpirationReason reason)
    {
        _key = key;
        _data = data;
        _reason = reason;
    }

    public KeyType Key
    {
        get { return _key; }
    }

    public DataType? Data
    {
        get { return _data; }
    }

    public ExpirationReason Reason
    {
        get { return _reason; }
    }
}

public interface ICachedData<KT, DT> 
    where KT: notnull
    where DT : class
{
    int CachedItemsCount { get; }
    TimeSpan DefaultExpireTime { get; set; }
    bool DisposeOfData { get; set; }
    bool ExpireItems { get; set; }
    int MaximumCacheItemsCount { get; set; }
    bool NotifyExpiredWithData { get; set; }
    bool RenewOnCacheHit { get; set; }


    IEnumerable<KT> GetCacheKeys();
    IEnumerable<DT?> GetCacheValues();

    bool MaybeGetItem(KT key, out DT? value);
    bool MaybeGetAndExtend(KT key, Func<DT, bool> condition, out DT? value);
    bool ContainsKey(KT key);
    void RemoveItem(KT key);

    void Add(KT key, DT value, TimeSpan? expiration = null);
    void Clear();

    event EventHandler<ExpiredEventArgs<KT, DT?>>? DataExpired;
}

public class CacheLimitException : Exception
{
    public CacheLimitException()
    {
    }

    public CacheLimitException(string msg)
        : base(msg)
    {
    }
}