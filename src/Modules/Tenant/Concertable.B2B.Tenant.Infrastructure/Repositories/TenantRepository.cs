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

    public async Task<IReadOnlyList<TenantMembershipEntity>> ListMembershipsByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await context.Memberships.Where(m => m.TenantId == tenantId).ToListAsync(ct);

    public Task<TenantMembershipEntity?> FindMembershipAsync(Guid tenantId, Guid userId, CancellationToken ct = default) =>
        context.Memberships.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, ct);

    public Task<int> CountOwnersAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Memberships.CountAsync(m => m.TenantId == tenantId && m.Role == TenantRole.Owner, ct);

    public Task<bool> IsMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default) =>
        context.Memberships.AnyAsync(m => m.TenantId == tenantId && m.UserId == userId, ct);

    public void AddMembership(TenantMembershipEntity membership) => context.Memberships.Add(membership);

    public void RemoveMembership(TenantMembershipEntity membership) => context.Memberships.Remove(membership);

    public async Task<IReadOnlyList<TenantInvitationEntity>> ListInvitationsByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await context.Invitations.Where(i => i.TenantId == tenantId).ToListAsync(ct);

    public async Task<IReadOnlyList<TenantInvitationEntity>> ListPendingInvitationsByTenantAsync(Guid tenantId, DateTime now, CancellationToken ct = default) =>
        await context.Invitations
            .Where(i => i.TenantId == tenantId && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .ToListAsync(ct);

    public Task<TenantInvitationEntity?> GetInvitationByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<TenantInvitationEntity?> GetPendingInvitationByEmailAsync(Guid tenantId, string email, CancellationToken ct = default) =>
        context.Invitations.FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Email == email && i.Status == InvitationStatus.Pending, ct);

    public void AddInvitation(TenantInvitationEntity invitation) => context.Invitations.Add(invitation);

    public void RemoveInvitation(TenantInvitationEntity invitation) => context.Invitations.Remove(invitation);

    // Filter on the membership entity's own columns before projecting — a predicate over the projected
    // record doesn't translate, so any Where must sit on TenantMembershipEntity.
    private IQueryable<UserMembership> Project(IQueryable<TenantMembershipEntity> memberships) =>
        memberships.Join(
            context.Tenants,
            m => m.TenantId,
            t => t.Id,
            (m, t) => new UserMembership(m.TenantId, t.LegalName, t.Type, m.Role));
}
