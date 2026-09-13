using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Customer.Ticket.Contracts.Events;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class TicketSaleProcessor : IIntegrationEventHandler<TicketPurchasedEvent>
{
    private readonly ConcertDbContext context;
    private readonly ILogger<TicketSaleProcessor> logger;
    private readonly IBus bus;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public TicketSaleProcessor(
        ConcertDbContext context,
        ILogger<TicketSaleProcessor> logger,
        IBus bus,
        IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.context = context;
        this.logger = logger;
        this.bus = bus;
        this.outboxBehavior = outboxBehavior;
    }

    public async Task HandleAsync(TicketPurchasedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(TicketSaleProcessor), ct))
            return;

        try
        {
            await outboxBehavior.ExecuteAsync(async () =>
            {
                context.AddInboxMessage(envelope, nameof(TicketSaleProcessor));
                var concert = await context.Concerts.FirstOrDefaultAsync(c => c.Id == @event.ConcertId, ct);
                if (concert is null)
                {
                    logger.ConcertNotFoundForTicketSale(@event.ConcertId);
                    return;
                }

                concert.IncrementTicketsSold(1);
                await bus.PublishAsync(new TenantActivityRecordedEvent(new ActivityRecord(
                    $"ticket-sale:{envelope.MessageId}",
                    concert.VenueTenantId,
                    ActivityType.TicketSold,
                    envelope.OccurredAtUtc,
                    $"Ticket sold: \"{concert.Name}\" (now {concert.TicketsSold}/{concert.TotalTickets})",
                    null,
                    $"/_venue/my/concerts/concert/{concert.Id}")), ct);
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
