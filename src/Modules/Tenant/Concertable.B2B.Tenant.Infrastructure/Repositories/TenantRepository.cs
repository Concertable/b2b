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

    // Filter on the membership entity's own columns before projecting — a predicate over the projected
    // record doesn't translate, so any Where must sit on TenantMembershipEntity.
    private IQueryable<UserMembership> Project(IQueryable<TenantMembershipEntity> memberships) =>
        memberships.Join(
            context.Tenants,
            m => m.TenantId,
            t => t.Id,
            (m, t) => new UserMembership(m.TenantId, t.LegalName, t.Type, m.Role));
}
