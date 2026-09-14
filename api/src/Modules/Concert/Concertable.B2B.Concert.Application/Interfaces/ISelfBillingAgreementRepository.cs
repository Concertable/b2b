using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISelfBillingAgreementRepository : ITenantScopedRepository<SelfBillingAgreementEntity>
{
    /// <summary>Deliberately bypasses the ambient single-owner scoping to check an explicit
    /// (possibly third-party) tenant's in-force agreement — unlike every other member on this
    /// interface, which is scoped to the caller.</summary>
    Task<bool> ExistsCurrentByTenantIdAsync(
        Guid tenantId,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>The current tenant's in-force agreement — the latest acceptance whose expiry is still in the
    /// future — or <see langword="null"/> when none is in force. Scoped to the caller by the single-owner filter.</summary>
    Task<SelfBillingAgreementEntity?> GetCurrentAsync(DateTime nowUtc, CancellationToken ct = default);

    /// <summary>The current tenant's most recent acceptance regardless of expiry, or <see langword="null"/> when
    /// the caller has never granted one — the row that decides grant (never) vs renew (lapsed/nearing).</summary>
    Task<SelfBillingAgreementEntity?> GetLatestAsync(CancellationToken ct = default);
}
