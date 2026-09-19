using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Events;

public sealed record TenantDisplayChangedDomainEvent(TenantEntity Tenant) : IDomainEvent;
