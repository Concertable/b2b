using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Tenant.Contracts.Events;

[MessageType("concertable.b2b.tenant-display-changed.v1")]
public sealed record TenantDisplayChanged(
    Guid TenantId,
    long Version,
    string DisplayName) : IIntegrationEvent;
