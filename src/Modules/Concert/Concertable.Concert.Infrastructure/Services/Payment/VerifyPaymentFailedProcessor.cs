using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Payment;

internal class VerifyPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IConcertNotifier concertNotifier;
    private readonly ILogger<VerifyPaymentFailedProcessor> logger;

    public VerifyPaymentFailedProcessor(IConcertNotifier concertNotifier, ILogger<VerifyPaymentFailedProcessor> logger)
    {
        this.concertNotifier = concertNotifier;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Verify)
            return;

        var applicationId = int.Parse(@event.Metadata["applicationId"]);
        var venueManagerId = @event.Metadata["venueManagerId"];
        logger.LogWarning(
            "Verify payment failed for application {ApplicationId}: [{FailureCode}] {FailureMessage}",
            applicationId, @event.FailureCode, @event.FailureMessage);
        await concertNotifier.VerifyPaymentFailedAsync(venueManagerId, new { applicationId, @event.FailureMessage });
    }
}
