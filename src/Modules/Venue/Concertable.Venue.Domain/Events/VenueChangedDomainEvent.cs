namespace Concertable.Venue.Domain.Events;

public record VenueChangedDomainEvent(VenueEntity Venue) : IDomainEvent;

