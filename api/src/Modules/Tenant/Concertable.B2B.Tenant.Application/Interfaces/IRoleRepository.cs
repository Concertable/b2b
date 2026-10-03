using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Application.Interfaces;

internal interface IRoleRepository
{
    Task InsertPresetsAsync(Guid tenantId, CancellationToken ct = default);
    void Insert(TenantRoleDefinition role);
    Task<TenantRoleDefinition?> GetActiveByIdAsync(Guid tenantId, Guid roleId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantRoleDefinition>> ListActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantRoleDefinition>?> ResolveActiveAsync(
        Guid tenantId, IReadOnlyCollection<Guid> roleIds, CancellationToken ct = default);
    Task<bool> NameExistsAsync(Guid tenantId, string name, Guid? excludedId, CancellationToken ct = default);
    Task<bool> HasProtectedOwnerAsync(Guid tenantId, Guid membershipId, CancellationToken ct = default);
    Task<IReadOnlyList<RoleSummaryDto>> GetSummariesForMembershipAsync(
        Guid tenantId, Guid membershipId, CancellationToken ct = default);
    Task<IReadOnlyList<RoleSummaryDto>> GetSummariesForInvitationAsync(
        Guid tenantId, Guid invitationId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<RoleSummaryDto>>> GetSummariesForMembershipsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> membershipIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<RoleSummaryDto>>> GetSummariesForInvitationsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> invitationIds, CancellationToken ct = default);
}
