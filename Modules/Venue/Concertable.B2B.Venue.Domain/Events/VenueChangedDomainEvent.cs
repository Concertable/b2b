using Concertable.Kernel;

namespace Concertable.B2B.Venue.Domain.Events;

public record VenueChangedDomainEvent(VenueEntity Venue) : IDomainEvent;

