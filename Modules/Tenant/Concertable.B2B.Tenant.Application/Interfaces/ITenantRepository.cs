using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>The current user's membership joined to its tenant's persona — everything request-scoped
/// authority needs (active tenant, role, persona) in a single indexed read. One membership per user today.</summary>
internal sealed record ActiveMembership(Guid TenantId, TenantRole Role, TenantType Type);

internal interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    /// <summary>The caller's active membership — the source of truth for their tenant, role, and persona.</summary>
    Task<ActiveMembership?> GetActiveMembershipAsync(Guid userId, CancellationToken ct = default);
}
