using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISelfBillingAgreementRepository : ITenantScopedRepository<SelfBillingAgreementEntity>
{
    /// <summary>The current tenant's in-force agreement — the latest acceptance whose expiry is still in the
    /// future — or <see langword="null"/> when none is in force. Scoped to the caller by the single-owner filter.</summary>
    Task<SelfBillingAgreementEntity?> GetCurrentAsync(DateTime nowUtc, CancellationToken ct = default);
}
