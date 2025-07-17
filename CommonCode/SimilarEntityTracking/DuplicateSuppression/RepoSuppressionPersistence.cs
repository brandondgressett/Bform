using BFormDomain.Repository;

namespace BFormDomain.CommonCode.Logic.DuplicateSuppression;

/// <summary>
/// 
/// </summary>
public class RepoSuppressionPersistence :  ISuppressionPersistence
{
    /// <summary>
    /// 
    /// </summary>
    private readonly IRepository<SuppressedItem> _repo;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="repo"></param>
    public RepoSuppressionPersistence(IRepository<SuppressedItem> repo) =>
        _repo = repo;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="match"></param>
    /// <returns></returns>
    public async Task<IWillShutUp?> GetSuppressionInfo(ICanShutUp match)
    {
        // find any similar
        var (candidates, _) = await _repo.GetAllAsync(
                            si => si.TargetId == match.TargetId &&
                                    si.ComparisonType == match.ComparisonType &&
                                    si.ComparisonHash == match.ComparisonHash);
            
        SuppressedItem? theMatch = null!;
        if (candidates.Count > 1) // possible hash collision -- find the right one
        {
            theMatch = candidates
                .FirstOrDefault(candidate => candidate.ComparisonPropertyString == match.ComparisonPropertyString);
        }
        else 
            theMatch = candidates.FirstOrDefault();

        return theMatch;

        
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public async Task SuppressStartingNow(ICanShutUp item)
    {
        var existing = await GetSuppressionInfo(item);

        if (null == existing) // no record of this. Make one.
        {
            await _repo.CreateAsync(
                    new SuppressedItem
                    {
                        Id = Guid.NewGuid(),
                        ComparisonHash = item.ComparisonHash,
                        ComparisonPropertyString = item.ComparisonPropertyString,
                        ComparisonType = item.ComparisonType,
                        TargetId = item.TargetId,
                        SuppressionTimeMinutes = item.SuppressionTimeMinutes,
                        Version = 0,
                        SuppressionStartTime = DateTime.UtcNow 
                    });
            
        }
        else // update the suppression start time on the existing one.
        {
            existing.SuppressionStartTime = DateTime.UtcNow;
            var si = (SuppressedItem)existing; // a little evil, I know
            await _repo.UpdateIgnoreVersionAsync(si);
        }


    }
}

