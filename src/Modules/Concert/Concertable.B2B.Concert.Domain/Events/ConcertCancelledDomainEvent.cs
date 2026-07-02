using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Events;

public sealed record ConcertCancelledDomainEvent(int ConcertId) : IDomainEvent;
