namespace BFormDomain.CommonCode.Utility.CompletionTracking;

/// <summary>
/// Mock / Demo implementation. Must be a singleton.
/// </summary>
public class InMemoryTrackWorking : ITrackWorking
{
    private record WorkTelemetry(string Id, DateTime Expiration, int RefCount);
    
    private Dictionary<string, WorkTelemetry> _trackedWork = new();

    private void EnforceExpiration()
    {
        lock (_trackedWork)
        {
            var copy = _trackedWork.ToList();
            foreach (var item in copy)
            {
                if (DateTime.UtcNow > item.Value.Expiration)
                    _trackedWork.Remove(item.Key);
            }
        }
    }


    public Task BeginWork(string id, TimeSpan expirationTimeout, int refCountStart = 0)
    {
        EnforceExpiration();
        lock(_trackedWork)
            _trackedWork.Add(id, new WorkTelemetry(id, DateTime.UtcNow + expirationTimeout, refCountStart));
        return Task.CompletedTask;
    }

    public Task DecrementWork(string id, int refCount = 1)
    {
        EnforceExpiration();
        lock(_trackedWork)
        {
            if(_trackedWork.ContainsKey(id))
            {
                var item = _trackedWork[id];
                if(DateTime.UtcNow < item.Expiration)
                {
                    int newRefCount = Math.Max(item.RefCount - refCount, 0);
                    _trackedWork[id] = new WorkTelemetry(item.Id, item.Expiration, newRefCount);
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task IncrementWork(string id, int refCount = 1)
    {
        EnforceExpiration();
        lock (_trackedWork)
        {
            if (_trackedWork.ContainsKey(id))
            {
                var item = _trackedWork[id];
                if (DateTime.UtcNow < item.Expiration)
                {
                    int newRefCount = Math.Max(item.RefCount + refCount, 0);
                    _trackedWork[id] = new WorkTelemetry(item.Id, item.Expiration, newRefCount);
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task<bool> MaybeCompleteWork(string id)
    {
        EnforceExpiration();
        bool completed = false;
        lock (_trackedWork)
        {
            if (_trackedWork.ContainsKey(id))
            {
                var item = _trackedWork[id];
                if (DateTime.UtcNow < item.Expiration)
                {
                    if(item.RefCount <= 0)
                        completed = true;
                    _trackedWork.Remove(id);
                }
            }
        }

        return Task.FromResult(completed);
    }
}
