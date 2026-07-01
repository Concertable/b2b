using Concertable.Kernel;

namespace Concertable.B2B.Artist.Domain.Events;

public sealed record ArtistChangedDomainEvent(ArtistEntity Artist) : IDomainEvent;
