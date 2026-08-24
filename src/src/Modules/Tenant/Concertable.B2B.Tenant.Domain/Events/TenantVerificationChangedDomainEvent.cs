using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Events;

public sealed record TenantVerificationChangedDomainEvent(TenantVerificationEntity Verification) : IDomainEvent;
