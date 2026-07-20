using Concertable.B2B.Tenant.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Tenant.Application.Interfaces;

/// <summary>A membership joined to its tenant's persona + legal name — everything request-scoped authority needs
/// (active tenant, role, persona) plus the label/persona the switcher lists.</summary>
internal sealed record UserMembership(Guid TenantId, string LegalName, TenantType Type, TenantRole Role);

internal interface ITenantRepository : IRepository<TenantEntity, Guid>
{
    /// <summary>The caller's membership in a specific tenant — validates an <c>X-Tenant-Id</c> header against
    /// authority. Null = the caller doesn't belong to that tenant (the request then fails closed).</summary>
    Task<UserMembership?> GetMembershipAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

    /// <summary>All of the caller's memberships (unordered) — feeds the single-membership default and the
    /// <c>/me</c> switcher payload.</summary>
    Task<IReadOnlyList<UserMembership>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Every membership row of a tenant — the members-management list (mapped to emails via <c>IUserModule</c>).</summary>
    Task<IReadOnlyList<TenantMembershipEntity>> ListMembershipsByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>A single tracked membership row to mutate (change role) or remove; null if the user isn't a member.</summary>
    Task<TenantMembershipEntity?> FindMembershipAsync(Guid tenantId, Guid userId, CancellationToken ct = default);

    /// <summary>Owners currently in the tenant — the last-Owner invariant reads this before a demote/remove.</summary>
    Task<int> CountOwnersAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Whether the user already belongs to the tenant — guards duplicate invitation-accept.</summary>
    Task<bool> IsMemberAsync(Guid tenantId, Guid userId, CancellationToken ct = default);

    void AddMembership(TenantMembershipEntity membership);
    void RemoveMembership(TenantMembershipEntity membership);

    /// <summary>Every invitation row of a tenant — the delete-org cascade removes them so no invitation outlives its tenant.</summary>
    Task<IReadOnlyList<TenantInvitationEntity>> ListInvitationsByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Pending invitations for a tenant — the members-management "pending invites" list.</summary>
    Task<IReadOnlyList<TenantInvitationEntity>> ListPendingInvitationsByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>A single tracked invitation to mutate (accept/revoke); null if it doesn't exist.</summary>
    Task<TenantInvitationEntity?> GetInvitationByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Whether a live (pending) invitation already exists for <c>(tenant, email)</c> — guards a duplicate invite.</summary>
    Task<bool> PendingInvitationExistsAsync(Guid tenantId, string email, CancellationToken ct = default);

    void AddInvitation(TenantInvitationEntity invitation);

    void RemoveInvitation(TenantInvitationEntity invitation);
}
