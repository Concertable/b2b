using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class EscrowPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IConcertWorkflowModule concertWorkflowModule;
    private readonly ConcertDbContext context;
    private readonly ILogger<EscrowPaymentProcessor> logger;

    public EscrowPaymentProcessor(
        IConcertWorkflowModule concertWorkflowModule,
        ConcertDbContext context,
        ILogger<EscrowPaymentProcessor> logger)
    {
        this.concertWorkflowModule = concertWorkflowModule;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Escrow)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(EscrowPaymentProcessor), ct))
            return;

        var bookingId = int.Parse(@event.Metadata["bookingId"]);
        logger.EscrowWebhookReceived(@event.TransactionId, bookingId);

        context.AddInboxMessage(envelope, nameof(EscrowPaymentProcessor));

        try
        {
            await concertWorkflowModule.EscrowSucceededAsync(bookingId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
