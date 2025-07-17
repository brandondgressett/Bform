namespace BFormDomain.CommonCode.Utility.CompletionTracking;

#if LATER
/// <summary>
/// Sometime, someday
/// </summary>
public class RedisTrackWorking : ITrackWorking
{
    public Task BeginWork(string id, TimeSpan expirationTimeout, int refCountStart = 0)
    {
        throw new NotImplementedException();
    }

    public Task DecrementWork(string id, int refCount = 1)
    {
        throw new NotImplementedException();
    }

    public Task IncrementWork(string id, int refCount = 1)
    {
        throw new NotImplementedException();
    }

    Task<bool> MaybeCompleteWork(string id)
    {
        throw new NotImplementedException();
    }
}
#endif