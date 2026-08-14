using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class VerifyPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IVerifyCoordinator coordinator;
    private readonly ConcertTenantDbContext context;
    private readonly ILogger<VerifyPaymentFailedProcessor> logger;

    public VerifyPaymentFailedProcessor(
        IVerifyCoordinator coordinator,
        ConcertTenantDbContext context,
        ILogger<VerifyPaymentFailedProcessor> logger)
    {
        this.coordinator = coordinator;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Verify)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VerifyPaymentFailedProcessor), ct))
            return;

        var applicationId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.ApplicationId);
        var venueManagerId = @event.Metadata.GetValue(PaymentMetadataKeys.VenueManagerId);
        logger.VerifyPaymentFailed(applicationId, @event.FailureCode, @event.FailureMessage);

        context.AddInboxMessage(envelope, nameof(VerifyPaymentFailedProcessor));

        try
        {
            await coordinator.FailedAsync(applicationId, venueManagerId, @event.FailureMessage, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
