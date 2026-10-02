namespace Concertable.B2B.Application.Contracts;

public interface IApplicationModule
{
    Task<int> GetVenuePendingCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<int> GetArtistPendingCountAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, int>> GetCountsByOpportunityIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<IReadOnlySet<int>> GetOpportunityIdsForArtistTenantAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
}
