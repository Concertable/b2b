using Concertable.Payment.Application.Interfaces.Webhook;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockStripeApiClientFail : IStripeApiClient
{
    public Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options) =>
        Task.FromResult(new PaymentIntent
        {
            Id = $"pi_test_{Guid.NewGuid():N}",
            Status = "requires_payment_method",
            Metadata = options.Metadata ?? []
        });

    public Task<Transfer> CreateTransferAsync(TransferCreateOptions options) =>
        throw new StripeException("Mock transfer failure");

    public Task<Refund> CreateRefundAsync(RefundCreateOptions options) =>
        throw new StripeException("Mock refund failure");

    public Task<TransferReversal> CreateTransferReversalAsync(string transferId, TransferReversalCreateOptions options) =>
        throw new StripeException("Mock transfer reversal failure");
}
