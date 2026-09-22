using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.DataAccess.Application;
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
    private readonly IOpportunityReadDbContext readDbContext;
    private readonly ITenantScope tenantScope;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;

    public OpportunityCancellationIntegrationEventHandler(
        OpportunityDbContext context,
        IOpportunityReadDbContext readDbContext,
        ITenantScope tenantScope,
        IUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.context = context;
        this.readDbContext = readDbContext;
        this.tenantScope = tenantScope;
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
            // The event names only the opportunity, so the owner comes off the row itself through the
            // unfiltered read stance; the transition then runs as that tenant, where the filter can see it.
            var tenantId = await readDbContext.Opportunities
                .Where(value => value.Id == opportunityId)
                .Select(value => (Guid?)value.TenantId)
                .SingleOrDefaultAsync(ct);
            if (tenantId is null)
                return;

            using var acting = tenantScope.As(tenantId.Value);

            var opportunity = await context.Opportunities
                .SingleOrDefaultAsync(value => value.Id == opportunityId, ct);
            opportunity?.Reopen();
        }, ct);
}
