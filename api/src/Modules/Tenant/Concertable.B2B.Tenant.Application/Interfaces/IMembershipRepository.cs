using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Authorization.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal sealed record UserMembership(
    MembershipSnapshot Snapshot,
    string LegalName,
    IReadOnlyList<RoleSummary> Roles,
    IReadOnlyList<TenantBusinessActivityKind> BusinessActivities);

internal interface IMembershipRepository : IRepository<TenantMembershipEntity, Guid>
{
    Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByIdsForShareAsync(
        IReadOnlyCollection<Guid> membershipIds,
        CancellationToken ct = default);

    Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByIdsForUpdateAsync(
        IReadOnlyCollection<Guid> membershipIds,
        CancellationToken ct = default);

    Task<AuthoritySnapshot?> GetAuthoritySnapshotByUserIdAndTenantIdAsync(
        Guid userId, Guid tenantId, CancellationToken ct = default);

    Task<MembershipSnapshot?> GetSnapshotByMembershipIdAsync(
        Guid membershipId, CancellationToken ct = default);

    Task<UserMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<UserMembership>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<TenantMembershipEntity>> ListMembershipsByTenantAsync(
        Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByTenantIdsAsync(
        IReadOnlyCollection<Guid> tenantIds, CancellationToken ct = default);

    Task<TenantMembershipEntity?> FindMembershipAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default);

    Task<TenantMembershipEntity?> FindMembershipByIdAsync(
        Guid tenantId, Guid membershipId, CancellationToken ct = default);

    Task<int> CountOwnersAsync(Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<TenantMembershipEntity>> ListAssignedToRoleAsync(
        Guid tenantId, Guid roleId, CancellationToken ct = default);

    Task<bool> IsMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
}
