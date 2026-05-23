using Concertable.Messaging.Contracts;

namespace Concertable.Concert.Contracts.Events;

public record ConcertApplicationCreatedEvent(
    int LifecycleId,
    int OpportunityId,
    int ArtistId,
    int ApplicationId) : IIntegrationEvent;
