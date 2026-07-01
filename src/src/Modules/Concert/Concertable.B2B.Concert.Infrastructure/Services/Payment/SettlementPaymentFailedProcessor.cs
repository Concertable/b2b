using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly ISettlementDispatcher settlementDispatcher;
    private readonly ConcertDbContext context;
    private readonly ILogger<SettlementPaymentFailedProcessor> logger;

    public SettlementPaymentFailedProcessor(
        ISettlementDispatcher settlementDispatcher,
        ConcertDbContext context,
        ILogger<SettlementPaymentFailedProcessor> logger)
    {
        this.settlementDispatcher = settlementDispatcher;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Settlement)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentFailedProcessor), ct))
            return;

        var bookingId = int.Parse(@event.Metadata["bookingId"]);
        logger.BookingPaymentFailed(bookingId, @event.FailureCode, @event.FailureMessage);

        context.AddInboxMessage(envelope, nameof(SettlementPaymentFailedProcessor));

        try
        {
            await settlementDispatcher.FailedAsync(bookingId);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
