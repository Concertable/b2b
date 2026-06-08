using Concertable.Kernel;

namespace Concertable.B2B.Venue.Domain.Events;

public sealed record VenueChangedDomainEvent(VenueEntity Venue) : IDomainEvent;

