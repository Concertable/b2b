using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain;

/// <summary>
/// Binds a user (Auth <c>sub</c>) to a tenant with exactly one role — the source of truth for "who may act
/// in this tenant" now that authority lives in B2B's own data, not in token claims. Unique per
/// <c>(TenantId, UserId)</c>; a user may hold memberships in many tenants. Authorization is derived per
/// request from the role's catalog bundle, so a role change or removal takes effect on the next request.
/// </summary>
public sealed class TenantMembershipEntity : IGuidEntity
{
    private TenantMembershipEntity() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>The member's Auth <c>sub</c>. A plain primitive FK — Auth owns the identity, B2B owns membership.</summary>
    public Guid UserId { get; private set; }
    public TenantRole Role { get; private set; }

    /// <summary><see langword="null"/> for the founding Owner; otherwise the inviter who created the invitation.</summary>
    public Guid? InvitedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static TenantMembershipEntity Create(Guid tenantId, Guid userId, TenantRole role, Guid? invitedBy, DateTime at) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Role = role,
            InvitedByUserId = invitedBy,
            CreatedAt = at,
        };

    /// <summary>The last-Owner invariant is enforced by the service layer, not here — a membership can't see its peers.</summary>
    public void ChangeRole(TenantRole role) => Role = role;
}
