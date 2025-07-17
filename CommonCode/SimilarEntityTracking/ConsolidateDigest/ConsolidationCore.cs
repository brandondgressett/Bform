using BFormDomain.CommonCode.Utility;
using BFormDomain.HelperClasses;
using BFormDomain.Validation;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace BFormDomain.CommonCode.Logic.ConsolidateDigest;

/// <summary>
/// Provides core logic for the consolidation service to consolidate
/// items into a persistent digest entity over time.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ConsolidationCore<T> : IConsolidationCore 
    where T : class, IDigestible, new()
{

    private readonly ConsolidatedDigestRepository _repo;
    private string _exchangeName;
    private string _qName;

    public ConsolidationCore(ConsolidatedDigestRepository repo)
    {
        _repo = repo;

        _exchangeName = $"consolidate_digest_{typeof(T).GetFriendlyTypeName()}";
        _qName = $"{typeof(T).GetFriendlyTypeName()}";

    }

    public string ExchangeName => _exchangeName;

    public string QueueName => _qName;


    /// <summary>
    /// Records the given item into the corresponding digest
    /// that matches its properties into persistent storage.
    /// </summary>
    /// <param name="item">The item to consolidate</param>
    /// <returns>A task</returns>
    public async Task ConsolidateAppendAsync(IDigestible item)
    {
        var existing = await _repo.Load(item);
        var mongoItem = new DigestEntryModel
        {
            InvocationTime = DateTime.UtcNow,
            Entry = JObject.Parse(item.DigestBodyJson).ToBsonObject()
        };

        if (null == existing)
        {
            existing = new ConsolidatedDigest
            {
                Id = Guid.NewGuid(),
                DigestUntil = item.DigestUntil,
                HeadLimit = item.HeadLimit,
                TailLimit = item.TailLimit,
                ForwardToExchange = item.ForwardToExchange,
                ForwardToRoute = item.ForwardToRoute,
                TargetId = item.TargetId,
                ComparisonType = item.ComparisonType,
                ComparisonHash = item.ComparisonHash,
                ComparisonPropertyString = item.ComparisonPropertyString,
                Version = 0
            };

            DigestModel.SpilloverAppend(existing.Head, existing.HeadLimit,
                                   existing.Tail, existing.TailLimit,
                                   mongoItem);

            await _repo.CreateAsync(existing);
        }
        else
        {
           

            // retry in case of concurrent updates, which should not interfere with the given logic.
            Retry.This(() =>
            {
                existing = AsyncHelper.RunSync(()=>_repo.Load(item));
                existing.Requires().IsNotNull();
                DigestModel.SpilloverAppend(existing!.Head, existing.HeadLimit,
                                       existing.Tail, existing.TailLimit,
                                       mongoItem);

                if (DateTime.UtcNow > existing.DigestUntil)
                    existing.Complete = true;

                _repo.Update(existing);
            }, limit: 5, sleep: 150);
        }
    }

    /// <summary>
    /// Gets all completed digests, ready to send to digest receivers.
    /// </summary>
    /// <returns>A list of completed digests.</returns>
    public async Task<List<IConsolidatedDigest>> GetCompletedDigestsAsync()
    {
        var (data, ctx) = await _repo.GetAllAsync(cd => cd.DigestUntil < DateTime.UtcNow);
        return data.Select(it => it as IConsolidatedDigest).ToList();
    }

    /// <summary>
    /// Loads a digest matching the given item.
    /// </summary>
    /// <param name="item">I digestible item.</param>
    /// <returns>A digest that should append the item.</returns>
    public async Task<IConsolidatedDigest?> GetConsolidatedDigestAsync(IDigestible item)
    {
        item.Requires().IsNotNull();
        return await _repo.Load(item);
    }

    /// <summary>
    /// Deletes the given set of digests that have already been completely processed.
    /// </summary>
    /// <param name="digests">A list of digests to delete.</param>
    /// <returns>A task.</returns>
    public async Task GroomCompletedDigests(IList<IConsolidatedDigest> digests)
    {
        var ids = digests.Select(it => it.Id).ToList();
        if(ids.Any())
            await _repo.DeleteFilterAsync(cd => ids.Contains(cd.Id));
    }
}
