using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly ISettlementExecutor settlementExecutor;
    private readonly ConcertTenantDbContext context;
    private readonly ILogger<SettlementPaymentProcessor> logger;

    public SettlementPaymentProcessor(
        ISettlementExecutor settlementExecutor,
        ConcertTenantDbContext context,
        ILogger<SettlementPaymentProcessor> logger)
    {
        this.settlementExecutor = settlementExecutor;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Settlement)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentProcessor), ct))
            return;

        var bookingId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.BookingId);
        logger.SettlementWebhookReceived(@event.TransactionId, bookingId);

        context.AddInboxMessage(envelope, nameof(SettlementPaymentProcessor));

        try
        {
            await settlementExecutor.SucceededAsync(bookingId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
