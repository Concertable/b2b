using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Events;

public sealed record ConcertPostedDomainEvent(int ConcertId) : IDomainEvent;
