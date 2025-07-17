namespace BFormDomain.CommonCode.Utility.CompletionTracking;

public interface ITrackWorking
{
    Task BeginWork(string id, TimeSpan expirationTimeout, int refCountStart = 0);
    Task IncrementWork(string id, int refCount = 1);
    Task DecrementWork(string id, int refCount = 1);
    Task<bool> MaybeCompleteWork(string id);
}
