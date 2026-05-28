using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

[MessageType("concertable.b2b.concert-application-created.v1")]
public record ConcertApplicationCreatedEvent(
    int LifecycleId,
    int OpportunityId,
    int ArtistId,
    int ApplicationId) : IIntegrationEvent;
