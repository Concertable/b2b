using Concertable.Kernel;

namespace Concertable.Concert.Domain.Events;

public record ConcertPostedDomainEvent(int ConcertId) : IDomainEvent;
