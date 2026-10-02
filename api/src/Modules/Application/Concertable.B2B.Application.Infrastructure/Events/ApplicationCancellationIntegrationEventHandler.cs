using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Events;

internal sealed class ApplicationCancellationIntegrationEventHandler :
    IIntegrationEventHandler<BookingCancelledEvent>,
    IIntegrationEventHandler<ConcertCancelledEvent>
{
    private readonly ApplicationPrivilegedDbContext context;
    private readonly IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior;

    public ApplicationCancellationIntegrationEventHandler(
        ApplicationPrivilegedDbContext context,
        IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.context = context;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public Task HandleAsync(
        BookingCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(
            @event.ApplicationId,
            ApplicationNotification.BookingCancelled,
            envelope,
            ct);

    public Task HandleAsync(
        ConcertCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(
            @event.ApplicationId,
            ApplicationNotification.ConcertCancelled,
            envelope,
            ct);

    private Task ProcessAsync(
        int applicationId,
        ApplicationNotification notification,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(ApplicationCancellationIntegrationEventHandler);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            var application = await context.Applications
                .SingleOrDefaultAsync(value => value.Id == applicationId, ct);
            application?.NotifyCounterparty(notification);
        }, ct);
}
