using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly ISettlementExecutor settlementExecutor;
    private readonly TenantConcertDbContext context;
    private readonly ILogger<SettlementPaymentFailedProcessor> logger;

    public SettlementPaymentFailedProcessor(
        ISettlementExecutor settlementExecutor,
        TenantConcertDbContext context,
        ILogger<SettlementPaymentFailedProcessor> logger)
    {
        this.settlementExecutor = settlementExecutor;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Settlement)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentFailedProcessor), ct))
            return;

        var bookingId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.BookingId);
        logger.BookingPaymentFailed(bookingId, @event.FailureCode, @event.FailureMessage);

        context.AddInboxMessage(envelope, nameof(SettlementPaymentFailedProcessor));

        try
        {
            await settlementExecutor.FailedAsync(bookingId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
