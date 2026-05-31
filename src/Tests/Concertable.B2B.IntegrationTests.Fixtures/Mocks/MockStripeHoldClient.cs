using Concertable.Payment.Application.Interfaces;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal class MockStripeHoldClient : IStripeHoldClient
{
    private readonly MockStripeApiClient stripeApiClient;

    public MockStripeHoldClient(MockStripeApiClient stripeApiClient)
    {
        this.stripeApiClient = stripeApiClient;
    }

    public Task<string> FindHeldIntentAsync(string stripeCustomerId, int applicationId, CancellationToken ct = default) =>
        Task.FromResult(stripeApiClient.LastPaymentIntentId);

    public Task CancelAsync(string intentId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task CaptureAsync(string intentId, IDictionary<string, string> metadata, CancellationToken ct = default)
    {
        stripeApiClient.UpdateLastMetadata(metadata);
        return Task.CompletedTask;
    }
}
