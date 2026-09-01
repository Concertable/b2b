using Concertable.B2B.Concert.Application.Interfaces;
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
    private readonly ISettlementService settlementService;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;
    private readonly ILogger<SettlementPaymentFailedProcessor> logger;

    public SettlementPaymentFailedProcessor(
        ConcertDbContext context,
        ISettlementService settlementService,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior,
        ILogger<SettlementPaymentFailedProcessor> logger)
    {
        this.context = context;
        this.settlementService = settlementService;
        this.outboxUnitOfWorkBehavior = outboxUnitOfWorkBehavior;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Settlement)
            return;

        var bookingId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.BookingId);
        var operationId = @event.Metadata.GetValueAs<Guid>(PaymentMetadataKeys.OperationId);
        logger.BookingPaymentFailed(bookingId, @event.FailureCode, @event.FailureMessage);
        var concertId = await context.Concerts
            .AsNoTracking()
            .Where(value => value.BookingId == bookingId)
            .Select(value => (int?)value.Id)
            .SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Settlement booking {bookingId} has no concert.");
        await settlementService.RecordFailureAsync(
            concertId,
            operationId,
            @event.TransactionId,
            @event.FailureCode ?? "unknown",
            @event.FailureMessage ?? "Settlement payment failed.",
            ct);

        try
        {
            await outboxUnitOfWorkBehavior.ExecuteAsync(async () =>
            {
                if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentFailedProcessor), ct))
                    return;

                context.AddInboxMessage(envelope, nameof(SettlementPaymentFailedProcessor));
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
