namespace Concertable.Concert.Domain.Events;

public record BookingSettledDomainEvent(int BookingId, ContractType ContractType) : IDomainEvent;
