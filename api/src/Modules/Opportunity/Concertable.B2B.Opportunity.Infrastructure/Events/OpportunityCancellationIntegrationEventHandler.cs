using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Events;

internal sealed class OpportunityCancellationIntegrationEventHandler :
    IIntegrationEventHandler<BookingCancelledEvent>,
    IIntegrationEventHandler<ConcertCancelledEvent>
{
    private readonly OpportunityDbContext context;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;

    public OpportunityCancellationIntegrationEventHandler(
        OpportunityDbContext context,
        IUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.context = context;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public Task HandleAsync(
        BookingCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OpportunityId, envelope, ct);

    public Task HandleAsync(
        ConcertCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OpportunityId, envelope, ct);

    private Task ProcessAsync(
        int opportunityId,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(OpportunityCancellationIntegrationEventHandler);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            var opportunity = await context.Opportunities
                .SingleOrDefaultAsync(value => value.Id == opportunityId, ct);
            opportunity?.Reopen();
        }, ct);
}
