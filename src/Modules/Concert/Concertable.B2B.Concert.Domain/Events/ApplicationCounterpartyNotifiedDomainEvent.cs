using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Events;

public enum ApplicationNotification
{
    Applied,
    Accepted,
    Withdrawn,
    Rejected,
    Cancelled
}

public sealed record ApplicationCounterpartyNotifiedDomainEvent(
    Guid RecipientTenantId,
    ApplicationNotification Kind) : IDomainEvent;
