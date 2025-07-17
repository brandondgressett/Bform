using System.Collections.Concurrent;

namespace BFormDomain.CommonCode.Utility.Caching;

public class InMemoryCachedData<TKeyType, TDataType> : ICachedData<TKeyType, TDataType>
    where TKeyType:notnull
    where TDataType : class
{
    private readonly ConcurrentDictionary<TKeyType, CacheValue<TDataType?>> _cacheTable = new();


    private readonly TimeSpan _groomScheduleTimeOut = new TimeSpan(0, 15, 0);

    private DateTime _nextGroomSchedule;

    public InMemoryCachedData(
        bool expireItems = false, 
        bool renewOnCacheHit = false, 
        bool disposeOfData = false,
        TimeSpan? defaultExpireTime = null, 
        int maximumCacheItemsCount = 0, 
        bool notifyExpiredWithData = true)
    {

        ExpireItems = expireItems;
        RenewOnCacheHit = renewOnCacheHit;
        DisposeOfData = disposeOfData;
        if (defaultExpireTime.HasValue)
            DefaultExpireTime = defaultExpireTime.Value;
        else
            DefaultExpireTime = TimeSpan.FromSeconds(3600);

        MaximumCacheItemsCount = maximumCacheItemsCount;
        NotifyExpiredWithData = notifyExpiredWithData;

        _nextGroomSchedule = DateTime.UtcNow + _groomScheduleTimeOut;
    }

    #region ICachedData<TKeyType,TDataType> Members

    public int CachedItemsCount
    {
        get { return _cacheTable.Count; }
    }

    public bool ExpireItems { get; set; }

    public bool DisposeOfData { get; set; }

    public bool RenewOnCacheHit { get; set; }

    public TimeSpan DefaultExpireTime { get; set; }

    public int MaximumCacheItemsCount { get; set; }


    public void Clear()
    {
        MaybeDisposeAllData();
        _cacheTable.Clear();
    }

    public bool NotifyExpiredWithData { get; set; }

    public bool MaybeGetItem(TKeyType key, out TDataType? value)
    {
        GroomAllExpiredItems();
        value = default(TDataType);

        bool found = _cacheTable.TryGetValue(key, out CacheValue<TDataType?>? item);

        if (found)
        {

            value = item!.Value;
            if (RenewOnCacheHit)
            {
                item.RenewExpiration();

            }
        }

        return found;
    }

    public bool MaybeGetAndExtend(TKeyType key, Func<TDataType, bool> condition, out TDataType? value)
    {
        bool found = false;
        value = default(TDataType);

         
        found = _cacheTable.TryGetValue(key, out CacheValue<TDataType?>? item);

        if (found)
        {
            value = item!.Value;
            if (condition(value!))
            {
                item.RenewExpiration();

            }
        }

        GroomAllExpiredItems();

        return found;
    }

    public bool ContainsKey(TKeyType key)
    {
        var t = typeof(TKeyType);

        if (Nullable.GetUnderlyingType(t) is not null)
        {
            if (key.Equals(default(TKeyType)))
            {

                return false; // NO
            }
        }

        GroomAllExpiredItems();

        return _cacheTable.ContainsKey(key);
    }

    public void RemoveItem(TKeyType key)
    {
        var t = typeof(TKeyType);

        if (Nullable.GetUnderlyingType(t) is not null)
        {
            if (key.Equals(default(TKeyType)))
            {

                return; // NO
            }
        }



        MaybeDisposeData(key);
        
        _cacheTable.TryRemove(key, out CacheValue<TDataType?>? _);
    }

    public void Add(TKeyType key, TDataType value, TimeSpan? expiration = null)
    {
        var t = typeof(TKeyType);

        if (Nullable.GetUnderlyingType(t) != null)
        {
            if (key.Equals(default(TKeyType)))
            {

                return; // NO
            }
        }

        if (expiration == null)
            expiration = DefaultExpireTime;

        GroomAllExpiredItems();

        if (_cacheTable.ContainsKey(key))
        {
            _cacheTable.TryRemove(key, out CacheValue<TDataType?>? _);
        }

        if (MaximumCacheItemsCount != 0 && MaximumCacheItemsCount <= _cacheTable.Count)
        {
            if (ExpireItems)
            {

                while (MaximumCacheItemsCount <= _cacheTable.Count)
                    MaybeExpireOldestItem();
            }
            else
            {
                string err =
                    string.Format("Cache limit of {0} exceeded, cannot add more items. Automatic grooming OFF.",
                                  _cacheTable.Count);

                throw new CacheLimitException(err);
            }
        }

        var cv = new CacheValue<TDataType?>
        {
            ExpirationLife = expiration.Value,
            ExpirationTime = DateTime.UtcNow + expiration.Value,
            Value = value
        };

        _cacheTable.AddOrUpdate(key, _ => cv, (k, v) => cv);


    }

    public event EventHandler<ExpiredEventArgs<TKeyType, TDataType?>>? DataExpired;

    public IEnumerable<TKeyType> GetCacheKeys()
    {
        return _cacheTable.Keys.ToArray();
    }

    public IEnumerable<TDataType?> GetCacheValues()
    {
        return _cacheTable.Values.Select(it => it.Value).ToArray();
    }

    #endregion

    private void DoNotifyDataExpired(TKeyType key, TDataType? data, ExpirationReason reason)
    {
        EventHandler<ExpiredEventArgs<TKeyType, TDataType?>>? ev = DataExpired;
        if (ev is not null)
        {
            if (!NotifyExpiredWithData)
                data = default;
            ev(this, new ExpiredEventArgs<TKeyType, TDataType?>(key, data, reason));
        }
    }

    private void MaybeExpireOldestItem()
    {
        if (ExpireItems)
        {

            try
            {
                var oldestItemSearch = (from item in _cacheTable
                                        let et = (null == item.Value) ? DateTime.MinValue : item.Value.ExpirationTime
                                        orderby et
                                        select item.Key).ToArray();


                if (oldestItemSearch.Any())
                {
                    var overage = 1;
                    if (MaximumCacheItemsCount != 0)
                    {
                        overage = oldestItemSearch.Length - MaximumCacheItemsCount;
                        overage = Math.Max(overage, MaximumCacheItemsCount / 10);
                    }
                    var iterations = Math.Max(1, overage);
                    for (int each = 0; each != iterations; each += 1)
                    {
                        var oldestItem = oldestItemSearch.ElementAt(each);


                        MaybeDisposeData(oldestItem);

                        if (_cacheTable.TryRemove(oldestItem, 
                            out InMemoryCachedData<TKeyType, TDataType>.CacheValue<TDataType?>? val))
                            DoNotifyDataExpired(oldestItem, val.Value, ExpirationReason.RemoveOldestForSpace);
                    }
                }

            }
            // concurrency bugs in concurrent dictionary. 
            // try again next time
            catch (NullReferenceException)
            {

            }
            catch (ArgumentException)
            {

            }
        }
    }

    private void GroomAllExpiredItems(bool now = false)
    {
        bool timeToGroom = now ||
                           DateTime.UtcNow > _nextGroomSchedule;

        if (ExpireItems && timeToGroom)
        {
            var currentTime = DateTime.UtcNow;
            try
            {

                TKeyType[] expiredItems =
                    _cacheTable.Keys.Where(k => _cacheTable[k].ExpirationTime < currentTime).ToArray();


                Array.ForEach(expiredItems, k =>
                {
                    MaybeDisposeData(k);
                    CacheValue<TDataType?>? val;

                    if (_cacheTable.TryRemove(k, out val))
                        DoNotifyDataExpired(k, val.Value,
                                            ExpirationReason.LifeTimeExceeded);
                });
                _nextGroomSchedule = DateTime.UtcNow + _groomScheduleTimeOut;
            }
            catch (Exception)
            {


            }
        }
    }

    private void MaybeDisposeData(TKeyType key)
    {
        if (DisposeOfData && !NotifyExpiredWithData)
        {
            try
            {
                if (_cacheTable[key] is IDisposable idisp)
                {

                    idisp.Dispose();
                }
            }
            catch (Exception)
            {

            }
        }
    }

    private void MaybeDisposeAllData()
    {
        if (DisposeOfData)
        {
            foreach (TKeyType k in _cacheTable.Keys)
                MaybeDisposeData(k);
        }
    }

    #region Nested type: CacheValue

    internal class CacheValue<TDt> 
    {
        public TDt? Value { get; set; }
        public DateTime ExpirationTime { get; set; }
        public TimeSpan ExpirationLife { get; set; }

        public void RenewExpiration()
        {
            ExpirationTime = DateTime.UtcNow + ExpirationLife;
        }
    }

    #endregion
}
