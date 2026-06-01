using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Events;

public sealed record BookingSettledDomainEvent(int BookingId, ContractType ContractType) : IDomainEvent;
