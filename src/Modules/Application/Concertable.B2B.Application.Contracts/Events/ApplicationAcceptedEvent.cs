using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Application.Contracts.Events;

[MessageType("concertable.b2b.application-accepted.v1")]
public sealed record ApplicationAcceptedEvent(
    int OpportunityId,
    Guid VenueTenantId) : IIntegrationEvent;
