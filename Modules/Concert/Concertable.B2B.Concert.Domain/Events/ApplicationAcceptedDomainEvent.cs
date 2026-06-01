using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Events;

public sealed record ApplicationAcceptedDomainEvent(int ApplicationId, int OpportunityId) : IDomainEvent;
