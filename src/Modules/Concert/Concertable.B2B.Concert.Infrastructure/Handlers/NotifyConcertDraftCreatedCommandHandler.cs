using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Handlers;

internal sealed class NotifyConcertDraftCreatedCommandHandler :
    IIntegrationCommandHandler<NotifyConcertDraftCreatedCommand>
{
    private readonly IConcertNotifier notifier;

    public NotifyConcertDraftCreatedCommandHandler(IConcertNotifier notifier)
    {
        this.notifier = notifier;
    }

    public async Task HandleAsync(
        NotifyConcertDraftCreatedCommand command,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        await notifier.ConcertDraftCreatedAsync(command.ArtistUserId.ToString(), command.ConcertId);
        await notifier.ConcertDraftCreatedAsync(command.VenueUserId.ToString(), command.ConcertId);
    }
}
