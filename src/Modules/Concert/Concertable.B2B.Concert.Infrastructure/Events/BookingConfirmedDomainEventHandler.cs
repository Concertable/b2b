using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Infrastructure.Emails;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Infrastructure.Events;

/// <summary>Pre-commit so the staged sends enlist in the booking's transaction; delegates composition and
/// outbox staging to <see cref="BookingConfirmationEmailSender"/>.</summary>
internal sealed class BookingConfirmedDomainEventHandler : IPreCommitDomainEventHandler<BookingConfirmedDomainEvent>
{
    private readonly BookingConfirmationEmailSender sender;

    public BookingConfirmedDomainEventHandler(BookingConfirmationEmailSender sender) => this.sender = sender;

    public Task HandleAsync(BookingConfirmedDomainEvent e, CancellationToken ct = default) => sender.SendAsync(e, ct);
}
