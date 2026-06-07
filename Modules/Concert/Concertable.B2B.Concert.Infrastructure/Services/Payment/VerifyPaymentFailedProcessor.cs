using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class VerifyPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IVerifyDispatcher verifyDispatcher;
    private readonly ConcertDbContext context;
    private readonly ILogger<VerifyPaymentFailedProcessor> logger;

    public VerifyPaymentFailedProcessor(
        IVerifyDispatcher verifyDispatcher,
        ConcertDbContext context,
        ILogger<VerifyPaymentFailedProcessor> logger)
    {
        this.verifyDispatcher = verifyDispatcher;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Verify)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VerifyPaymentFailedProcessor), ct))
            return;

        var applicationId = int.Parse(@event.Metadata["applicationId"]);
        var venueManagerId = @event.Metadata["venueManagerId"];
        logger.VerifyPaymentFailed(applicationId, @event.FailureCode, @event.FailureMessage);

        context.AddInboxMessage(envelope, nameof(VerifyPaymentFailedProcessor));

        try
        {
            await verifyDispatcher.VerifyFailedAsync(applicationId, venueManagerId, @event.FailureMessage);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
