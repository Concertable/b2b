using Concertable.Kernel;

namespace Concertable.B2B.Application.Domain.Events;

public enum ApplicationNotification
{
    Applied,
    Accepted,
    Withdrawn,
    Rejected
}

public sealed record ApplicationCounterpartyNotifiedDomainEvent(
    Guid RecipientTenantId,
    ApplicationNotification Kind) : IDomainEvent;
