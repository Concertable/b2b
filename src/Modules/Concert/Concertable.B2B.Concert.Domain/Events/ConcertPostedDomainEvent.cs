using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Events;

public record ConcertPostedDomainEvent(int ConcertId) : IDomainEvent;
