using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Events;

/// <summary>Raised when a tenant is created; the pre-commit handler turns it into the integration event.</summary>
public sealed record TenantCreatedDomainEvent(Guid TenantId, Guid CreatedByUserId, string Email) : IDomainEvent;
