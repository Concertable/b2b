using System.Collections.Immutable;
using System.Data;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class MembershipRepository : Repository<TenantMembershipEntity>, IMembershipRepository,
    IMembershipReadRepository
{
    private readonly TenantDbContext context;
    private readonly UnitOfWorkAccessor unitOfWorkAccessor;

    public MembershipRepository(
        TenantDbContext context,
        UnitOfWorkAccessor unitOfWorkAccessor) : base(context)
    {
        this.context = context;
        this.unitOfWorkAccessor = unitOfWorkAccessor;
    }

    public async Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByIdsForShareAsync(
        IReadOnlyCollection<Guid> membershipIds,
        CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Membership locking requires an active transaction.");
        await unitOfWork.EnlistAsync(context, ct);
        var distinctIds = membershipIds.Distinct().Order().ToArray();
        foreach (var membershipId in distinctIds)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 SELECT 1
                 FROM tenant."Memberships"
                 WHERE "Id" = {membershipId}
                 FOR SHARE
                 """,
                ct);
        }
        var members = await context.Memberships.AsNoTracking()
            .Where(member => distinctIds.Contains(member.Id)).ToListAsync(ct);
        return await MaterializeAsync(members, ct);
    }

    public async Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByIdsForUpdateAsync(
        IReadOnlyCollection<Guid> membershipIds, CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Membership updates require an active transaction.");
        await unitOfWork.EnlistAsync(context, ct);
        var distinctIds = membershipIds.Distinct().Order().ToArray();
        foreach (var membershipId in distinctIds)
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 SELECT 1
                 FROM tenant."Memberships"
                 WHERE "Id" = {membershipId}
                 FOR UPDATE
                 """,
                ct);
        }
        var members = await context.Memberships.AsNoTracking()
            .Where(member => distinctIds.Contains(member.Id)).ToListAsync(ct);
        return await MaterializeAsync(members, ct);
    }

    public Task<MembershipSnapshot?> GetSnapshotByMembershipIdAsync(
        Guid membershipId, CancellationToken ct = default) =>
        ReadWithFenceAsync(async readCt =>
        {
            var tenantId = await context.Memberships.AsNoTracking()
                .Where(member => member.Id == membershipId)
                .Select(member => (Guid?)member.TenantId).SingleOrDefaultAsync(readCt);
            if (tenantId is null)
                return null;
            await LockTenantForShareAsync(tenantId.Value, readCt);
            var members = await context.Memberships.AsNoTracking()
                .Where(member => member.Id == membershipId && member.TenantId == tenantId)
                .ToListAsync(readCt);
            return (await MaterializeAsync(members, readCt)).SingleOrDefault();
        }, ct);

    public Task<AuthoritySnapshot?> GetAuthoritySnapshotByUserIdAndTenantIdAsync(
        Guid userId, Guid tenantId, CancellationToken ct = default) =>
        ReadWithFenceAsync(async readCt =>
        {
            await LockTenantForShareAsync(tenantId, readCt);
            var members = await context.Memberships.AsNoTracking()
                .Where(member => member.UserId == userId && member.TenantId == tenantId)
                .ToListAsync(readCt);
            var actor = (await MaterializeAsync(members, readCt)).SingleOrDefault();
            if (actor is null)
                return null;
            var revision = await context.AuthorizationCatalogStates.AsNoTracking()
                .Where(state => state.Id == 1)
                .Select(state => state.Revision)
                .SingleOrDefaultAsync(readCt);
            return revision is null ? null : new AuthoritySnapshot(actor, revision);
        }, ct);

    public Task<MembershipSnapshot?> GetSnapshotByUserIdAndTenantIdAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        ReadWithFenceAsync(async ct =>
        {
            await LockTenantForShareAsync(tenantId, ct);
            var members = await context.Memberships.AsNoTracking()
                .Where(member => member.UserId == userId && member.TenantId == tenantId)
                .ToListAsync(ct);
            return (await MaterializeAsync(members, ct)).SingleOrDefault();
        }, cancellationToken);

    public Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        ReadByUserIdAsync(userId, (snapshots, _) =>
            Task.FromResult(snapshots), cancellationToken);

    public async Task<UserMembership?> GetMembershipAsync(
        Guid userId, Guid tenantId, CancellationToken ct = default) =>
        (await GetMembershipsAsync(userId, ct)).SingleOrDefault(member => member.Snapshot.TenantId == tenantId);

    public Task<IReadOnlyList<UserMembership>> GetMembershipsAsync(
        Guid userId, CancellationToken ct = default) =>
        ReadByUserIdAsync(userId, ToUserMembershipsAsync, ct);

    public async Task<IReadOnlyList<TenantMembershipEntity>> ListMembershipsByTenantAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await context.Memberships.Where(member => member.TenantId == tenantId).ToListAsync(ct);

    public Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByTenantIdsAsync(
        IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default) =>
        ReadWithFenceAsync(async readCt =>
        {
            var distinctIds = tenantIds.Distinct().Order().ToArray();
            foreach (var tenantId in distinctIds)
                await LockTenantForShareAsync(tenantId, readCt);
            var members = await context.Memberships.AsNoTracking()
                .Where(member => distinctIds.Contains(member.TenantId)).ToListAsync(readCt);
            return await MaterializeAsync(members, readCt);
        }, ct);

    public Task<TenantMembershipEntity?> FindMembershipAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default) =>
        context.Memberships.FirstOrDefaultAsync(member => member.TenantId == tenantId && member.UserId == userId, ct);

    public Task<TenantMembershipEntity?> FindMembershipByIdAsync(
        Guid tenantId, Guid membershipId, CancellationToken ct = default) =>
        context.Memberships.FirstOrDefaultAsync(
            member => member.TenantId == tenantId && member.Id == membershipId, ct);

    public Task<int> CountOwnersAsync(Guid tenantId, CancellationToken ct = default) =>
        context.MembershipRoleAssignments
            .CountAsync(row => row.TenantId == tenantId
                && context.RoleDefinitions.Any(role => role.TenantId == tenantId
                    && role.Id == row.RoleId && role.IsProtectedOwner && role.RetiredAt == null), ct);

    public async Task<IReadOnlyList<TenantMembershipEntity>> ListAssignedToRoleAsync(
        Guid tenantId, Guid roleId, CancellationToken ct = default) =>
        await context.Memberships.Where(member => member.TenantId == tenantId
            && member.Assignments.Any(row => row.RoleId == roleId)).ToListAsync(ct);

    public Task<bool> IsMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default) =>
        context.Memberships.AnyAsync(member => member.TenantId == tenantId && member.UserId == userId, ct);

    private async Task<IReadOnlyList<MembershipSnapshot>> MaterializeAsync(
        IReadOnlyList<TenantMembershipEntity> members,
        CancellationToken ct)
    {
        if (members.Count == 0)
            return [];
        var tenantIds = members.Select(member => member.TenantId).Distinct().ToArray();
        var tenants = await context.Tenants.AsNoTracking()
            .Where(tenant => tenantIds.Contains(tenant.Id))
            .ToDictionaryAsync(tenant => tenant.Id, ct);
        var roleIds = members.SelectMany(member => member.Assignments.Select(row => row.RoleId)).Distinct().ToArray();
        var roles = await context.RoleDefinitions.AsNoTracking()
            .Where(role => roleIds.Contains(role.Id) && role.RetiredAt == null)
            .ToDictionaryAsync(role => role.Id, ct);
        return members.Select(member =>
        {
            var effective = member.Assignments
                .Where(row => roles.TryGetValue(row.RoleId, out var role) && role.TenantId == member.TenantId)
                .SelectMany(row => roles[row.RoleId].Permissions)
                .Where(row => TenantPermission.TryParse(row.PermissionKey, out _)
                    && row.Audience is ResourceAudience.AssignedResources or ResourceAudience.TenantResources)
                .GroupBy(row => row.PermissionKey, StringComparer.Ordinal)
                .ToImmutableDictionary(
                    group => TenantPermission.TryParse(group.Key, out var key) ? key : default,
                    group => (ResourceAudience)group.Max(row => (int)row.Audience));
            return new MembershipSnapshot(
                member.Id, member.TenantId, member.UserId, member.PermissionVersion,
                tenants[member.TenantId].RolePolicyVersion, effective);
        }).ToList();
    }

    private async Task<IReadOnlyList<UserMembership>> ToUserMembershipsAsync(
        IReadOnlyList<MembershipSnapshot> snapshots, CancellationToken ct)
    {
        var tenantIds = snapshots.Select(snapshot => snapshot.TenantId).Distinct().ToArray();
        var tenants = await context.Tenants.AsNoTracking()
            .Where(tenant => tenantIds.Contains(tenant.Id)).ToDictionaryAsync(tenant => tenant.Id, ct);
        var membershipIds = snapshots.Select(snapshot => snapshot.MembershipId).ToArray();
        var members = await context.Memberships.AsNoTracking()
            .Where(member => membershipIds.Contains(member.Id))
            .ToListAsync(ct);
        var roleIds = members.SelectMany(member => member.Assignments.Select(row => row.RoleId)).Distinct().ToArray();
        var roles = await context.RoleDefinitions.AsNoTracking()
            .Where(role => roleIds.Contains(role.Id)).ToDictionaryAsync(role => role.Id, ct);
        return snapshots.Select(snapshot =>
        {
            var member = members.Single(row => row.Id == snapshot.MembershipId);
            var tenant = tenants[snapshot.TenantId];
            return new UserMembership(
                snapshot,
                tenant.LegalName,
                [.. member.Assignments.Select(row => roles[row.RoleId])
                    .Select(role => new RoleSummary(role.Id, role.Name, role.IsProtectedOwner))],
                [.. tenant.BusinessActivities.Where(activity => activity.IsActive).Select(activity => activity.Kind)]);
        }).ToList();
    }

    private Task<TResult> ReadByUserIdAsync<TResult>(
        Guid userId,
        Func<IReadOnlyList<MembershipSnapshot>, CancellationToken, Task<TResult>> project,
        CancellationToken ct) =>
        ReadWithFenceAsync(async readCt =>
        {
            var tenantIds = await context.Memberships.AsNoTracking()
                .Where(member => member.UserId == userId)
                .Select(member => member.TenantId).Distinct().Order().ToListAsync(readCt);
            foreach (var tenantId in tenantIds)
                await LockTenantForShareAsync(tenantId, readCt);
            var members = await context.Memberships.AsNoTracking()
                .Where(member => member.UserId == userId && tenantIds.Contains(member.TenantId))
                .ToListAsync(readCt);
            var snapshots = await MaterializeAsync(members, readCt);
            return await project(snapshots, readCt);
        }, ct);

    private async Task<TResult> ReadWithFenceAsync<TResult>(
        Func<CancellationToken, Task<TResult>> read, CancellationToken ct)
    {
        if (unitOfWorkAccessor.Current is { } unitOfWork)
        {
            await unitOfWork.EnlistAsync(context, ct);
            return await read(ct);
        }

        await using var transaction = await BeginStandaloneReadAsync(ct);
        var result = await read(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    private Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginStandaloneReadAsync(
        CancellationToken ct) =>
        context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

    private Task<int> LockTenantForShareAsync(Guid tenantId, CancellationToken ct) =>
        context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM tenant."Tenants"
             WHERE "Id" = {tenantId}
             FOR SHARE
             """, ct);
}
