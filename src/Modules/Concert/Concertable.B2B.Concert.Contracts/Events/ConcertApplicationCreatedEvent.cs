using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Events;

public record ConcertApplicationCreatedEvent(
    int LifecycleId,
    int OpportunityId,
    int ArtistId,
    int ApplicationId) : IIntegrationEvent;
