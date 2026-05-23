using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Events;

public record ConcertChangedDomainEvent(
    int ConcertId,
    int TotalTickets,
    decimal Price,
    DateRange Period,
    DateTime? DatePosted) : IDomainEvent;
