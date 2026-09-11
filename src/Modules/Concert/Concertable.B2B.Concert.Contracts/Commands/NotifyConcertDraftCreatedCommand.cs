using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Contracts.Commands;

[MessageType("concertable.b2b.notify-concert-draft-created.v1")]
public sealed record NotifyConcertDraftCreatedCommand(
    int ConcertId,
    Guid ArtistUserId,
    Guid VenueUserId) : IIntegrationCommand;
