using Concertable.B2B.Concert.Domain.Lifecycle;
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
    private readonly ConcertDbContext context;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;
    private readonly ILogger<SettlementPaymentProcessor> logger;
    private readonly IBus bus;

    public SettlementPaymentProcessor(
        ConcertDbContext context,
        IOutboxUnitOfWorkBehavior outboxBehavior,
        ILogger<SettlementPaymentProcessor> logger,
        IBus bus)
    {
        this.context = context;
        this.outboxBehavior = outboxBehavior;
        this.logger = logger;
        this.bus = bus;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Settlement)
            return;

        var bookingId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.BookingId);
        logger.SettlementWebhookReceived(@event.TransactionId, bookingId);

        try
        {
            await outboxBehavior.ExecuteAsync(async () =>
            {
                if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentProcessor), ct))
                    return;

                context.AddInboxMessage(envelope, nameof(SettlementPaymentProcessor));
                var concert = await context.Concerts.SingleOrDefaultAsync(value => value.BookingId == bookingId, ct)
                    ?? throw new InvalidOperationException($"Settlement booking {bookingId} has no concert.");

                if (concert.State is State.Complete)
                {
                    if (concert.FinancialOperationReferenceId != @event.TransactionId)
                        throw new InvalidOperationException(
                            $"Concert {concert.Id} completed settlement {concert.FinancialOperationReferenceId}, not {@event.TransactionId}.");
                    return;
                }

                if (concert.CompleteSettlement(@event.TransactionId).TryGetError(out var transitionError))
                    throw new InvalidOperationException($"Concert cannot complete settlement from {transitionError.Current}.");
                await PublishActivityAsync(concert.VenueTenantId, "venue", concert, envelope, ct);
                await PublishActivityAsync(concert.ArtistTenantId, "artist", concert, envelope, ct);
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
