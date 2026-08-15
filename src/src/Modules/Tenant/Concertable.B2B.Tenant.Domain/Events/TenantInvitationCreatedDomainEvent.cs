using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Events;

/// <summary>Raised when an invitation is created; the pre-commit handler stages the invitation email so it
/// commits atomically with the invitation row. Carries <see cref="TenantType"/> so the handler can build the
/// venue-or-artist portal accept link without re-reading the tenant during the save.</summary>
public sealed record TenantInvitationCreatedDomainEvent(
    Guid InvitationId,
    string Email,
    TenantRole Role,
    TenantType TenantType) : IDomainEvent;
