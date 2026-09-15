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

    /// <summary>GDPR erasure gate: whether any of the subject's tenants has an application that has committed money
    /// but not yet reached a booking, so erasure defers rather than corrupting settlement. Fail-closed and answered
    /// tenant-less by explicit ids.</summary>
    Task<bool> HasLiveObligationsAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);
}
