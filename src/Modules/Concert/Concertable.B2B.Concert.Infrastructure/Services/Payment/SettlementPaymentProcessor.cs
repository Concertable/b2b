using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal class SettlementPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IConcertWorkflowModule concertWorkflowModule;
    private readonly ConcertDbContext context;
    private readonly ILogger<SettlementPaymentProcessor> logger;

    public SettlementPaymentProcessor(
        IConcertWorkflowModule concertWorkflowModule,
        ConcertDbContext context,
        ILogger<SettlementPaymentProcessor> logger)
    {
        this.concertWorkflowModule = concertWorkflowModule;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Settlement)
            return;

        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(SettlementPaymentProcessor), ct))
            return;

        var bookingId = int.Parse(@event.Metadata["bookingId"]);
        logger.SettlementWebhookReceived(@event.TransactionId, bookingId);

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(SettlementPaymentProcessor), envelope.MessageType, DateTimeOffset.UtcNow));

        try
        {
            await concertWorkflowModule.SettleAsync(bookingId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
