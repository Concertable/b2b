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
    private readonly OpportunityPrivilegedDbContext context;
    private readonly IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior;

    public OpportunityCancellationIntegrationEventHandler(
        OpportunityPrivilegedDbContext context,
        IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.context = context;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public Task HandleAsync(
        BookingCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OpportunityId, @event.ApplicationId, envelope, ct);

    public Task HandleAsync(
        ConcertCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        ProcessAsync(@event.OpportunityId, @event.ApplicationId, envelope, ct);

    private Task ProcessAsync(
        int opportunityId,
        int applicationId,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(OpportunityCancellationIntegrationEventHandler);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            await LockOpportunityAsync(opportunityId, ct);
            var opportunity = await context.Opportunities
                .SingleOrDefaultAsync(value => value.Id == opportunityId, ct);
            opportunity?.CancelApplication(applicationId);
        }, ct);

    private Task LockOpportunityAsync(int opportunityId, CancellationToken ct) =>
        context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM opportunity."Opportunities"
             WHERE "Id" = {opportunityId}
             FOR UPDATE
             """,
            ct);
}
