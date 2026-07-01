using Concertable.Testing.Integration;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public sealed class MockStripeApiClient : IResettable
{
    public string LastPaymentIntentId { get; private set; } = null!;
    public string LastEventId { get; private set; } = null!;
    public Dictionary<string, string> LastMetadata { get; private set; } = [];

    public void Reset()
    {
        LastPaymentIntentId = null!;
        LastEventId = null!;
        LastMetadata = [];
    }

    public void UpdateLastMetadata(IDictionary<string, string> metadata)
    {
        LastMetadata = new Dictionary<string, string>(metadata);
    }

    public Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options)
    {
        LastPaymentIntentId = $"pi_test_{Guid.NewGuid():N}";
        LastEventId = $"evt_test_{Guid.NewGuid():N}";
        LastMetadata = options.Metadata ?? [];

        return Task.FromResult(new PaymentIntent
        {
            Id = LastPaymentIntentId,
            Status = "succeeded",
            AmountReceived = options.Amount ?? 0,
            Metadata = options.Metadata ?? []
        });
    }
}
