using Concertable.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Payment;

internal class VerifyPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IConcertNotifier concertNotifier;
    private readonly ConcertDbContext context;
    private readonly ILogger<VerifyPaymentFailedProcessor> logger;

    public VerifyPaymentFailedProcessor(
        IConcertNotifier concertNotifier,
        ConcertDbContext context,
        ILogger<VerifyPaymentFailedProcessor> logger)
    {
        this.concertNotifier = concertNotifier;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Verify)
            return;

        if (await context.Set<InboxMessageEntity>().AnyAsync(
            m => m.MessageId == envelope.MessageId && m.ConsumerName == nameof(VerifyPaymentFailedProcessor), ct))
            return;

        var applicationId = int.Parse(@event.Metadata["applicationId"]);
        var venueManagerId = @event.Metadata["venueManagerId"];
        logger.LogWarning(
            "Verify payment failed for application {ApplicationId}: [{FailureCode}] {FailureMessage}",
            applicationId, @event.FailureCode, @event.FailureMessage);

        await concertNotifier.VerifyPaymentFailedAsync(venueManagerId, new { applicationId, @event.FailureMessage });

        context.Set<InboxMessageEntity>().Add(
            InboxMessageEntity.Create(envelope.MessageId, nameof(VerifyPaymentFailedProcessor), envelope.MessageType, DateTimeOffset.UtcNow));
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.LogDebug("Duplicate inbox message {MessageId}; skipping", envelope.MessageId);
        }
    }
}
