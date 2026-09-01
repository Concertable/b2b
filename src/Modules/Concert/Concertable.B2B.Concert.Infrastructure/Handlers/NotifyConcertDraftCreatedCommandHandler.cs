using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Handlers;

internal sealed class NotifyConcertDraftCreatedCommandHandler :
    IIntegrationCommandHandler<NotifyConcertDraftCreatedCommand>
{
    private readonly IConcertNotifier concertNotifier;

    public NotifyConcertDraftCreatedCommandHandler(IConcertNotifier concertNotifier)
    {
        this.concertNotifier = concertNotifier;
    }

    public async Task HandleAsync(
        NotifyConcertDraftCreatedCommand command,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        await concertNotifier.ConcertDraftCreatedAsync(command.ArtistUserId.ToString(), command.ConcertId);
        await concertNotifier.ConcertDraftCreatedAsync(command.VenueUserId.ToString(), command.ConcertId);
    }
}
