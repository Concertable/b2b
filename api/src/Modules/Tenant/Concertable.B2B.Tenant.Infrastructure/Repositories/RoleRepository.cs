using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class RoleRepository(TenantDbContext context) : IRoleRepository
{
    public Task InsertPresetsAsync(Guid tenantId, CancellationToken ct = default)
    {
        context.RoleDefinitions.AddRange(TenantRoleProvisioning.CreatePresets(tenantId));
        return Task.CompletedTask;
    }

    public void Insert(TenantRoleDefinition role) => context.RoleDefinitions.Add(role);

    public Task<TenantRoleDefinition?> GetActiveByIdAsync(
        Guid tenantId, Guid roleId, CancellationToken ct = default) =>
        context.RoleDefinitions.SingleOrDefaultAsync(role =>
            role.TenantId == tenantId && role.Id == roleId && role.RetiredAt == null, ct);

    public async Task<IReadOnlyList<TenantRoleDefinition>> ListActiveAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await context.RoleDefinitions.AsNoTracking()
            .Where(role => role.TenantId == tenantId && role.RetiredAt == null)
            .OrderBy(role => role.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<TenantRoleDefinition>?> ResolveActiveAsync(
        Guid tenantId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct = default)
    {
        if (roleIds.Count == 0 || roleIds.Any(id => id == Guid.Empty)
            || roleIds.Count != roleIds.Distinct().Count())
            return null;
        var roles = await context.RoleDefinitions
            .Where(role => role.TenantId == tenantId && roleIds.Contains(role.Id)
                && role.RetiredAt == null).ToListAsync(ct);
        return roles.Count == roleIds.Count ? roles : null;
    }

    public Task<bool> NameExistsAsync(
        Guid tenantId, string name, Guid? excludedId, CancellationToken ct = default) =>
        context.RoleDefinitions.AnyAsync(role =>
            role.TenantId == tenantId && role.Id != excludedId
            && role.Name.ToLower() == name.ToLower(), ct);

    public Task<bool> HasProtectedOwnerAsync(
        Guid tenantId, Guid membershipId, CancellationToken ct = default) =>
        context.MembershipRoleAssignments.AnyAsync(row =>
            row.TenantId == tenantId && row.MembershipId == membershipId
            && context.RoleDefinitions.Any(role =>
                role.TenantId == row.TenantId
                && role.Id == row.RoleId
                && role.IsProtectedOwner
                && role.RetiredAt == null), ct);

    public async Task<IReadOnlyList<RoleSummary>> GetSummariesForMembershipAsync(
        Guid tenantId, Guid membershipId, CancellationToken ct = default)
    {
        var roles = await context.RoleDefinitions.AsNoTracking()
            .Where(role => role.TenantId == tenantId
                && context.MembershipRoleAssignments.Any(row =>
                    row.TenantId == tenantId && row.MembershipId == membershipId
                    && row.RoleId == role.Id)).ToListAsync(ct);
        return Summaries(roles);
    }

    public async Task<IReadOnlyList<RoleSummary>> GetSummariesForInvitationAsync(
        Guid tenantId, Guid invitationId, CancellationToken ct = default)
    {
        var roles = await context.RoleDefinitions.AsNoTracking()
            .Where(role => role.TenantId == tenantId
                && context.InvitationRoleAssignments.Any(row =>
                    row.TenantId == tenantId && row.InvitationId == invitationId
                    && row.RoleId == role.Id)).ToListAsync(ct);
        return Summaries(roles);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<RoleSummary>>> GetSummariesForMembershipsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> membershipIds, CancellationToken ct = default)
    {
        var rows = await (from assignment in context.MembershipRoleAssignments.AsNoTracking()
            join role in context.RoleDefinitions.AsNoTracking()
                on new { assignment.TenantId, Id = assignment.RoleId }
                equals new { role.TenantId, role.Id }
            where assignment.TenantId == tenantId && membershipIds.Contains(assignment.MembershipId)
            select new { assignment.MembershipId, role.Id, role.Name, role.IsProtectedOwner }).ToListAsync(ct);
        return rows.GroupBy(row => row.MembershipId)
            .ToDictionary(group => group.Key,
                group => (IReadOnlyList<RoleSummary>)group
                    .Select(row => new RoleSummary(row.Id, row.Name, row.IsProtectedOwner))
                    .OrderBy(role => role.Name, StringComparer.Ordinal).ToArray());
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<RoleSummary>>> GetSummariesForInvitationsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> invitationIds, CancellationToken ct = default)
    {
        var rows = await (from assignment in context.InvitationRoleAssignments.AsNoTracking()
            join role in context.RoleDefinitions.AsNoTracking()
                on new { assignment.TenantId, Id = assignment.RoleId }
                equals new { role.TenantId, role.Id }
            where assignment.TenantId == tenantId && invitationIds.Contains(assignment.InvitationId)
            select new { assignment.InvitationId, role.Id, role.Name, role.IsProtectedOwner }).ToListAsync(ct);
        return rows.GroupBy(row => row.InvitationId)
            .ToDictionary(group => group.Key,
                group => (IReadOnlyList<RoleSummary>)group
                    .Select(row => new RoleSummary(row.Id, row.Name, row.IsProtectedOwner))
                    .OrderBy(role => role.Name, StringComparer.Ordinal).ToArray());
    }

    private static IReadOnlyList<RoleSummary> Summaries(
        IEnumerable<TenantRoleDefinition> roles) =>
        roles.Select(role => new RoleSummary(role.Id, role.Name, role.IsProtectedOwner))
            .OrderBy(role => role.Name, StringComparer.Ordinal).ToArray();
}
