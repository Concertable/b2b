using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Strategies;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface ITenantContactResolver : ITenantStrategy
{
    /// <summary>The contact for the venue or artist the given tenant owns — <see cref="Option{T}.None"/> when
    /// the tenant owns none (a data-integrity edge, not the ordinary case).</summary>
    Task<Option<TenantContact>> ResolveAsync(TenantType type, Guid tenantId, CancellationToken ct = default);
}
