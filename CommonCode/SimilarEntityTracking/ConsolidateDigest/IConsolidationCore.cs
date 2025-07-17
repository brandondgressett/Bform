
namespace BFormDomain.CommonCode.Logic.ConsolidateDigest
{
    public interface IConsolidationCore
    {
        Task ConsolidateAppendAsync(IDigestible item);
        Task<List<IConsolidatedDigest>> GetCompletedDigestsAsync();
        Task<IConsolidatedDigest?> GetConsolidatedDigestAsync(IDigestible item);
        Task GroomCompletedDigests(IList<IConsolidatedDigest> digests);

        string ExchangeName { get; }
        string QueueName { get; }
    }
}