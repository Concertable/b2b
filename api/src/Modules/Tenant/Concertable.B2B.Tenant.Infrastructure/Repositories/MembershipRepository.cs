using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Locking;
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

    public async Task<MembershipSnapshot?> GetSnapshotByIdWithKeyShareLockAsync(
        Guid membershipId,
        CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Membership locking requires an active transaction.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Memberships.LockRowsAsync(member => member.Id == membershipId, RowLock.KeyShare, ct);
        var members = await context.Memberships.AsNoTracking()
            .Where(member => member.Id == membershipId)
            .ToListAsync(ct);
        return (await MaterializeAsync(members, ct)).SingleOrDefault();
    }

    public async Task<MembershipSnapshot?> GetSnapshotByTenantIdAndIdAsync(
        Guid tenantId,
        Guid membershipId,
        CancellationToken ct = default)
    {
        var members = await context.Memberships
            .Where(member => member.TenantId == tenantId && member.Id == membershipId)
            .ToListAsync(ct);
        return (await MaterializeAsync(members, ct)).SingleOrDefault();
    }

    public async Task<MembershipSnapshot?> GetSnapshotByUserIdAndTenantIdAsync(
        Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        if (await EnlistInActiveUnitOfWorkAsync(ct))
            await FenceTenantDeletionAsync(new HashSet<Guid> { tenantId }, ct);
        var members = await context.Memberships.AsNoTracking()
            .Where(member => member.UserId == userId && member.TenantId == tenantId)
            .ToListAsync(ct);
        return (await MaterializeAsync(members, ct)).SingleOrDefault();
    }

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

    public Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByTenantIdAsync(
        Guid tenantId, CancellationToken ct = default) =>
        GetSnapshotsByTenantIdsAsync(new HashSet<Guid> { tenantId }, ct);

    public async Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByTenantIdsAsync(
        IReadOnlySet<Guid> tenantIds, CancellationToken ct = default)
    {
        if (await EnlistInActiveUnitOfWorkAsync(ct))
            await FenceTenantDeletionAsync(tenantIds, ct);
        var members = await context.Memberships.AsNoTracking()
            .Where(member => tenantIds.Contains(member.TenantId)).ToListAsync(ct);
        return await MaterializeAsync(members, ct);
    }

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
            var permissions = member.Assignments
                .Where(row => roles.TryGetValue(row.RoleId, out var role) && role.TenantId == member.TenantId)
                .Select(row => roles[row.RoleId])
                .ToPermissionSet();
            return new MembershipSnapshot(
                member.Id, member.TenantId, member.UserId, member.PermissionVersion,
                tenants[member.TenantId].RolePolicyVersion, permissions);
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

    private async Task<TResult> ReadByUserIdAsync<TResult>(
        Guid userId,
        Func<IReadOnlyList<MembershipSnapshot>, CancellationToken, Task<TResult>> project,
        CancellationToken ct)
    {
        var fenced = await EnlistInActiveUnitOfWorkAsync(ct);
        var tenantIds = await context.Memberships.AsNoTracking()
            .Where(member => member.UserId == userId)
            .Select(member => member.TenantId)
            .ToHashSetAsync(ct);
        if (fenced)
            await FenceTenantDeletionAsync(tenantIds, ct);
        var members = await context.Memberships.AsNoTracking()
            .Where(member => member.UserId == userId && tenantIds.Contains(member.TenantId))
            .ToListAsync(ct);
        var snapshots = await MaterializeAsync(members, ct);
        return await project(snapshots, ct);
    }

    private async Task<bool> EnlistInActiveUnitOfWorkAsync(CancellationToken ct)
    {
        if (unitOfWorkAccessor.Current is not { } unitOfWork)
            return false;

        await unitOfWork.EnlistAsync(context, ct);
        return true;
    }

    private Task FenceTenantDeletionAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct) =>
        context.Tenants.LockRowsAsync(tenant => tenantIds.Contains(tenant.Id), RowLock.KeyShare, ct);
}
