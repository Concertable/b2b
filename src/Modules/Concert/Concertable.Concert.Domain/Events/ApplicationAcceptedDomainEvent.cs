namespace Concertable.Concert.Domain.Events;

public record ApplicationAcceptedDomainEvent(int ApplicationId, int OpportunityId) : IDomainEvent;
