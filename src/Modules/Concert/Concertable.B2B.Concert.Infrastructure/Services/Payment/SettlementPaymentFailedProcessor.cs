using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly ConcertDbContext context;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;
    private readonly ILogger<SettlementPaymentFailedProcessor> logger;

    public SettlementPaymentFailedProcessor(
        ConcertDbContext context,
        IOutboxUnitOfWorkBehavior outboxBehavior,
        ILogger<SettlementPaymentFailedProcessor> logger)
    {
        this.context = context;
        this.outboxBehavior = outboxBehavior;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Settlement)
            return;

        var bookingId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.BookingId);
        logger.BookingPaymentFailed(bookingId, @event.FailureCode, @event.FailureMessage);

        try
        {
            await outboxBehavior.ExecuteAsync(async () =>
            {
                if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentFailedProcessor), ct))
                    return;

                context.AddInboxMessage(envelope, nameof(SettlementPaymentFailedProcessor));
                var concert = await context.Concerts.SingleOrDefaultAsync(value => value.BookingId == bookingId, ct)
                    ?? throw new InvalidOperationException($"Settlement booking {bookingId} has no concert.");

                if (concert.State is State.SettlementFailed)
                {
                    if (concert.FinancialOperationReferenceId != @event.TransactionId)
                        throw new InvalidOperationException(
                            $"Concert {concert.Id} failed settlement {concert.FinancialOperationReferenceId}, not {@event.TransactionId}.");
                    return;
                }

                if (concert.RecordSettlementFailure(
                    @event.TransactionId,
                    @event.FailureCode ?? "unknown",
                    @event.FailureMessage ?? "Settlement payment failed.").TryGetError(out var transitionError))
                    throw new InvalidOperationException($"Concert cannot record settlement failure from {transitionError.Current}.");
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
