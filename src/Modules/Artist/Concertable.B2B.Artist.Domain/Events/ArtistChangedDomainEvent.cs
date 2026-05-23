using Concertable.Kernel;

namespace Concertable.B2B.Artist.Domain.Events;

public record ArtistChangedDomainEvent(ArtistEntity Artist) : IDomainEvent;
