using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IContractRepository : IVenueArtistTenantScopedRepository<ContractEntity>
{
    /// <summary>
    /// Tenant-filtered: resolves the contract for the caller's own application, or null when the
    /// caller is not a party (the two-party filter hides it) — same 404-not-403 stance as reading
    /// the application itself.
    /// </summary>
    Task<ContractEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default);

    /// <summary>
    /// Tenant-filtered: the contract for the caller's own concert, or null for a non-party. Lets the
    /// concert page download the contract without knowing the application id.
    /// </summary>
    Task<ContractEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default);

    Task<int?> GetIdByApplicationIdAsync(int applicationId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, int>> GetContractIdsByApplicationIdsAsync(
        IReadOnlyCollection<int> applicationIds, CancellationToken ct = default);
}
