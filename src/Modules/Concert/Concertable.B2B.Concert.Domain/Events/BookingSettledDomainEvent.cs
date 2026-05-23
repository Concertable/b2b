using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Events;

public record BookingSettledDomainEvent(int BookingId, ContractType ContractType) : IDomainEvent;
