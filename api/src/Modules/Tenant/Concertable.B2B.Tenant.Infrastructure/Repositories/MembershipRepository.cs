using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Mappers;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class MembershipRepository : Repository<TenantMembershipEntity>, IMembershipRepository,
    IMembershipReadRepository, IMembershipAuthorityFence
{
    private readonly TenantDbContext context;
    private readonly CommandTransactionAccessor transactions;

    public MembershipRepository(
        TenantDbContext context,
        CommandTransactionAccessor transactions) : base(context)
    {
        this.context = context;
        this.transactions = transactions;
    }

    public async Task<MembershipSnapshot?> RequireCurrentAsync(
        MembershipSnapshot expected,
        CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Membership authority requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM tenant.Tenants WITH (HOLDLOCK)
             WHERE Id = {expected.TenantId}
             """,
            ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM tenant.Memberships WITH (HOLDLOCK)
             WHERE Id = {expected.MembershipId}
               AND TenantId = {expected.TenantId}
               AND UserId = {expected.UserId}
             """,
            ct);

        var current = await context.Memberships
            .Where(membership =>
                membership.Id == expected.MembershipId
                && membership.TenantId == expected.TenantId
                && membership.UserId == expected.UserId)
            .Select(membership => new MembershipSnapshot(
                membership.Id,
                membership.TenantId,
                membership.UserId,
                membership.Role,
                membership.PermissionVersion))
            .SingleOrDefaultAsync(ct);

        return current is { } authority
            && authority.Role == expected.Role
            && authority.PermissionVersion == expected.PermissionVersion
                ? authority
                : null;
    }

    public Task<MembershipSnapshot?> GetSnapshotByUserIdAndTenantIdAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        context.Memberships
            .Where(m => m.UserId == userId && m.TenantId == tenantId)
            .Select(m => new MembershipSnapshot(m.Id, m.TenantId, m.UserId, m.Role, m.PermissionVersion))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await context.Memberships
            .Where(m => m.UserId == userId)
            .Select(m => new MembershipSnapshot(m.Id, m.TenantId, m.UserId, m.Role, m.PermissionVersion))
            .ToListAsync(cancellationToken);

    public Task<UserMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default) =>
        context.Memberships
            .Where(m => m.UserId == userId && m.TenantId == tenantId)
            .ToUserMemberships(context.Tenants, context.BusinessProfiles)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<UserMembership>> GetMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        await context.Memberships
            .Where(m => m.UserId == userId)
            .ToUserMemberships(context.Tenants, context.BusinessProfiles)
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
