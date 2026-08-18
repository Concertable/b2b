using Concertable.B2B.Concert.Domain.State;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly ConcertDbContext context;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;
    private readonly ILogger<SettlementPaymentProcessor> logger;

    public SettlementPaymentProcessor(
        ConcertDbContext context,
        IOutboxUnitOfWorkBehavior outboxBehavior,
        ILogger<SettlementPaymentProcessor> logger)
    {
        this.context = context;
        this.outboxBehavior = outboxBehavior;
        this.logger = logger;
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

                if (concert.State is ConcertState.Complete)
                {
                    if (concert.FinancialOperationReferenceId != @event.TransactionId)
                        throw new InvalidOperationException(
                            $"Concert {concert.Id} completed settlement {concert.FinancialOperationReferenceId}, not {@event.TransactionId}.");
                    return;
                }

                concert.CompleteSettlement(@event.TransactionId);
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
