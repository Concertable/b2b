using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Events;

public sealed record TenantInvitationCreatedDomainEvent(
    Guid InvitationId,
    string Email,
    TenantRole Role,
    TenantType TenantType) : IDomainEvent;
