using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Tenant.Contracts.Events;

[MessageType("concertable.b2b.tenant-activity-recorded.v1")]
public sealed record TenantActivityRecordedEvent(ActivityRecord Activity) : IIntegrationEvent;
