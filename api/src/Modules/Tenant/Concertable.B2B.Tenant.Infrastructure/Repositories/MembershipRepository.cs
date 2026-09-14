using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class MembershipRepository : Repository<TenantMembershipEntity>, IMembershipRepository
{
    private readonly TenantDbContext context;

    public MembershipRepository(TenantDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<UserMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default) =>
        context.Memberships
            .Where(m => m.UserId == userId && m.TenantId == tenantId)
            .ToUserMemberships(context.Tenants)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserMembership>> GetMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        await context.Memberships
            .Where(m => m.UserId == userId)
            .ToUserMemberships(context.Tenants)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TenantMembershipEntity>> ListMembershipsByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await context.Memberships.Where(m => m.TenantId == tenantId).ToListAsync(ct);

    public Task<TenantMembershipEntity?> FindMembershipAsync(Guid tenantId, Guid userId, CancellationToken ct = default) =>
        context.Memberships.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, ct);

    public Task<int> CountOwnersAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Memberships.CountAsync(m => m.TenantId == tenantId && m.Role == TenantRole.Owner, ct);

    public Task<bool> IsMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default) =>
        context.Memberships.AnyAsync(m => m.TenantId == tenantId && m.UserId == userId, ct);
}
