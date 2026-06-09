using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Tenant.Contracts.Events;

[MessageType("concertable.b2b.tenant-created.v1")]
public sealed record TenantCreatedEvent(
    Guid TenantId,
    Guid CreatedByUserId,
    string Email) : IIntegrationEvent;
