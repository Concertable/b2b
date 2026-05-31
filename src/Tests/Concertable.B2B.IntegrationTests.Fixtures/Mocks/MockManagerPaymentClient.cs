using Concertable.Payment.Client;
using Concertable.Payment.Domain;
using FluentResults;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal class MockManagerPaymentClient : IManagerPaymentClient
{
    private readonly MockStripeApiClient stripeApiClient;

    public MockManagerPaymentClient(MockStripeApiClient stripeApiClient)
    {
        this.stripeApiClient = stripeApiClient;
    }

    public Task<Result<PaymentResponse>> PayAsync(Guid payerId, Guid payeeId, decimal amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Ok(new PaymentResponse { RequiresAction = false, TransactionId = "pi_mock_pay" }));

    public async Task<CheckoutSession> CreateSetupSessionAsync(Guid payerId, IDictionary<string, string> metadata, CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions { Metadata = new Dictionary<string, string>(metadata) });
        return new CheckoutSession(intent.Id + "_secret", "cuss_mock_secret", "cus_mock");
    }

    public async Task<CheckoutSession> CreateVerifySessionAsync(Guid payerId, IDictionary<string, string> metadata, CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions { Metadata = new Dictionary<string, string>(metadata) });
        return new CheckoutSession(intent.Id + "_secret", "cuss_mock_secret", "cus_mock");
    }

    public async Task<CheckoutSession> CreateHoldSessionAsync(Guid payerId, decimal amount, IDictionary<string, string> metadata, CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Metadata = new Dictionary<string, string>(metadata)
        });
        return new CheckoutSession(intent.Id + "_secret", "cuss_mock_secret", "cus_mock");
    }

    public Task<string> FindHeldIntentAsync(Guid payerId, int applicationId, CancellationToken ct = default) =>
        Task.FromResult(stripeApiClient.LastPaymentIntentId);
}
