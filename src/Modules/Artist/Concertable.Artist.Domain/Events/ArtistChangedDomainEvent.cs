using Concertable.Kernel;

namespace Concertable.Artist.Domain.Events;

public record ArtistChangedDomainEvent(ArtistEntity Artist) : IDomainEvent;
