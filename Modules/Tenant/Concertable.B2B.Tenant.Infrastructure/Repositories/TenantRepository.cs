using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class TenantRepository : Repository<TenantEntity>, ITenantRepository
{
    public TenantRepository(TenantDbContext context) : base(context) { }

    public Task<UserMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default) =>
        Project(context.Memberships.Where(m => m.UserId == userId && m.TenantId == tenantId)).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserMembership>> GetMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        await Project(context.Memberships.Where(m => m.UserId == userId)).ToListAsync(ct);

    public Task<Guid?> GetDefaultTenantIdAsync(Guid userId, CancellationToken ct = default) =>
        // Selects just the id off the membership's own columns — no join, so ordering on InvitedByUserId
        // translates: founding Owner (null) first, then a stable tie-break. Deterministic single tenant for the
        // transitional owner claim.
        context.Memberships
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.InvitedByUserId == null ? 0 : 1)
            .ThenBy(m => m.TenantId)
            .Select(m => (Guid?)m.TenantId)
            .FirstOrDefaultAsync(ct);

    // Filter on the membership entity's own columns before projecting — a predicate/order over the projected
    // record doesn't translate, so any Where must sit on TenantMembershipEntity.
    private IQueryable<UserMembership> Project(IQueryable<TenantMembershipEntity> memberships) =>
        memberships.Join(
            context.Tenants,
            m => m.TenantId,
            t => t.Id,
            (m, t) => new UserMembership(m.TenantId, t.LegalName, t.Type, m.Role, m.InvitedByUserId));
}
