using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly ISettlementExecutor settlementExecutor;
    private readonly ConcertDbContext context;
    private readonly ILogger<SettlementPaymentProcessor> logger;
    private readonly IBus bus;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public SettlementPaymentProcessor(
        ISettlementExecutor settlementExecutor,
        ConcertDbContext context,
        ILogger<SettlementPaymentProcessor> logger,
        IBus bus,
        IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.settlementExecutor = settlementExecutor;
        this.context = context;
        this.logger = logger;
        this.bus = bus;
        this.outboxBehavior = outboxBehavior;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Settlement)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentProcessor), ct))
            return;

        var bookingId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.BookingId);
        logger.SettlementWebhookReceived(@event.TransactionId, bookingId);

        try
        {
            await outboxBehavior.ExecuteAsync(async () =>
            {
                context.AddInboxMessage(envelope, nameof(SettlementPaymentProcessor));
                await settlementExecutor.SucceededAsync(bookingId, ct);
                var concert = await context.Concerts
                    .AsNoTracking()
                    .SingleOrDefaultAsync(c => c.BookingId == bookingId, ct);
                if (concert is not null)
                {
                    await PublishActivityAsync(concert.VenueTenantId, "venue", concert, envelope, ct);
                    await PublishActivityAsync(concert.ArtistTenantId, "artist", concert, envelope, ct);
                }
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }

    private Task PublishActivityAsync(
        Guid tenantId,
        string persona,
        ConcertEntity concert,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        bus.PublishAsync(new TenantActivityRecordedEvent(new ActivityRecord(
            $"settlement:{envelope.MessageId}",
            tenantId,
            ActivityType.ConcertSettled,
            envelope.OccurredAtUtc,
            $"\"{concert.Name}\" settled",
            null,
            $"/_{persona}/my/concerts/concert/{concert.Id}")), ct);
}
