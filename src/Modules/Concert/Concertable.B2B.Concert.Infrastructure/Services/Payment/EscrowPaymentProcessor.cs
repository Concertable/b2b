using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class EscrowPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IEscrowExecutor escrowExecutor;
    private readonly ConcertDbContext context;
    private readonly ILogger<EscrowPaymentProcessor> logger;

    public EscrowPaymentProcessor(
        IEscrowExecutor escrowExecutor,
        ConcertDbContext context,
        ILogger<EscrowPaymentProcessor> logger)
    {
        this.escrowExecutor = escrowExecutor;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Escrow)
            return;
        if (@event.Metadata.ContainsKey(PaymentMetadataKeys.OperationId))
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(EscrowPaymentProcessor), ct))
            return;

        var bookingId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.BookingId);
        logger.EscrowWebhookReceived(@event.TransactionId, bookingId);

        context.AddInboxMessage(envelope, nameof(EscrowPaymentProcessor));

        try
        {
            await escrowExecutor.SucceededAsync(bookingId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
