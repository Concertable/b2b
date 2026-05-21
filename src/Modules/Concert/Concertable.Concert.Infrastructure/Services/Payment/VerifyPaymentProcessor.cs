using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Payment;

internal class VerifyPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IConcertWorkflowModule concertWorkflowModule;
    private readonly ILogger<VerifyPaymentProcessor> logger;

    public VerifyPaymentProcessor(IConcertWorkflowModule concertWorkflowModule, ILogger<VerifyPaymentProcessor> logger)
    {
        this.concertWorkflowModule = concertWorkflowModule;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Verify)
            return;

        var applicationId = int.Parse(@event.Metadata["applicationId"]);
        logger.LogDebug(
            "Verify webhook received: payment intent {TransactionId} for application {ApplicationId}",
            @event.TransactionId, applicationId);
        await concertWorkflowModule.VerifyAsync(applicationId, ct);
    }
}
