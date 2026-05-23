using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal class TicketSaleProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly ConcertDbContext context;
    private readonly ILogger<TicketSaleProcessor> logger;

    public TicketSaleProcessor(ConcertDbContext context, ILogger<TicketSaleProcessor> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Ticket)
            return;

        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(TicketSaleProcessor), ct))
            return;

        var meta = @event.Metadata;
        var concertId = int.Parse(meta["concertId"]);
        var quantity = meta.TryGetValue("quantity", out var q) ? int.Parse(q) : 1;

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(TicketSaleProcessor), envelope.MessageType, DateTimeOffset.UtcNow));

        var concert = await context.Concerts.FirstOrDefaultAsync(c => c.Id == concertId, ct);
        if (concert is not null)
            concert.IncrementTicketsSold(quantity);
        else
            logger.LogWarning("Concert {ConcertId} not found for ticket sale", concertId);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.LogDebug("Duplicate inbox message {MessageId}; skipping", envelope.MessageId);
        }
    }
}
