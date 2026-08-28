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
    private readonly ApplicationDbContext context;
    private readonly IUnitOfWorkBehavior unitOfWork;

    public ApplicationCancellationIntegrationEventHandler(
        ApplicationDbContext context,
        IUnitOfWorkBehavior unitOfWork)
    {
        this.context = context;
        this.unitOfWork = unitOfWork;
    }

    public Task HandleAsync(
        BookingCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.ApplicationId, envelope, ct);

    public Task HandleAsync(
        ConcertCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.ApplicationId, envelope, ct);

    private Task ProcessAsync(
        int applicationId,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var handler = nameof(ApplicationCancellationIntegrationEventHandler);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            var application = await context.Applications
                .SingleOrDefaultAsync(value => value.Id == applicationId, ct);
            application?.NotifyCounterparty(ApplicationNotification.ConcertCancelled);
        }, ct);
}
