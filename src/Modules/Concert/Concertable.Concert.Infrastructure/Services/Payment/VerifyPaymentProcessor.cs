using Concertable.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Payment;

internal class VerifyPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IConcertWorkflowModule concertWorkflowModule;
    private readonly ConcertDbContext context;
    private readonly ILogger<VerifyPaymentProcessor> logger;

    public VerifyPaymentProcessor(
        IConcertWorkflowModule concertWorkflowModule,
        ConcertDbContext context,
        ILogger<VerifyPaymentProcessor> logger)
    {
        this.concertWorkflowModule = concertWorkflowModule;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Verify)
            return;

        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(VerifyPaymentProcessor), ct))
            return;

        var applicationId = int.Parse(@event.Metadata["applicationId"]);
        logger.LogDebug(
            "Verify webhook received: payment intent {TransactionId} for application {ApplicationId}",
            @event.TransactionId, applicationId);

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(VerifyPaymentProcessor), envelope.MessageType, DateTimeOffset.UtcNow));

        try
        {
            await concertWorkflowModule.VerifyAsync(applicationId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.LogDebug("Duplicate inbox message {MessageId}; skipping", envelope.MessageId);
        }
    }
}
