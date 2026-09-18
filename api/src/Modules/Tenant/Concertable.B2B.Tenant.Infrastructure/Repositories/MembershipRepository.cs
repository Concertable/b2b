using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Mappers;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class MembershipRepository : Repository<TenantMembershipEntity>, IMembershipRepository,
    IMembershipReadRepository, IMembershipAuthorityFence, ITenantCommandFacts
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

    public async Task<TenantCommandFacts?> ResolveAsync(
        MembershipSnapshot expectedActor,
        Guid targetTenantId,
        Guid? targetMembershipId = null,
        CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Tenant command facts require an active command transaction.");
        await transaction.EnlistAsync(context, ct);

        foreach (var tenantId in new[] { expectedActor.TenantId, targetTenantId }.Distinct().Order())
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 SELECT 1
                 FROM tenant.Tenants WITH (HOLDLOCK)
                 WHERE Id = {tenantId}
                 """,
                ct);
        }

        IEnumerable<Guid> membershipIds = targetMembershipId is { } targetId
            ? new[] { expectedActor.MembershipId, targetId }.Distinct().Order()
            : [expectedActor.MembershipId];
        foreach (var membershipId in membershipIds)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 SELECT 1
                 FROM tenant.Memberships WITH (HOLDLOCK)
                 WHERE Id = {membershipId}
                 """,
                ct);
        }

        var actor = await context.Memberships
            .Where(membership =>
                membership.Id == expectedActor.MembershipId
                && membership.TenantId == expectedActor.TenantId
                && membership.UserId == expectedActor.UserId)
            .Select(membership => new MembershipSnapshot(
                membership.Id,
                membership.TenantId,
                membership.UserId,
                membership.Role,
                membership.PermissionVersion))
            .SingleOrDefaultAsync(ct);
        if (actor is null
            || actor.Role != expectedActor.Role
            || actor.PermissionVersion != expectedActor.PermissionVersion)
            return null;

        var targetTenantExists = await context.Tenants.AnyAsync(tenant => tenant.Id == targetTenantId, ct);
        MembershipSnapshot? targetMembership = null;
        if (targetMembershipId is { } membershipIdValue)
        {
            targetMembership = await context.Memberships
                .Where(membership =>
                    membership.Id == membershipIdValue
                    && membership.TenantId == targetTenantId)
                .Select(membership => new MembershipSnapshot(
                    membership.Id,
                    membership.TenantId,
                    membership.UserId,
                    membership.Role,
                    membership.PermissionVersion))
                .SingleOrDefaultAsync(ct);
        }

        return new TenantCommandFacts(actor, targetTenantExists, targetMembership);
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
