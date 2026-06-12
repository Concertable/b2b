using Concertable.Payment.Application.Interfaces.Webhook;
using Concertable.Testing.Integration;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public sealed class MockStripeApiClient : IStripeApiClient, IResettable
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

    public Task<Transfer> CreateTransferAsync(TransferCreateOptions options) =>
        Task.FromResult(new Transfer
        {
            Id = $"tr_test_{Guid.NewGuid():N}",
            Amount = options.Amount ?? 0,
            DestinationId = options.Destination,
            SourceTransactionId = options.SourceTransaction,
            Metadata = options.Metadata ?? []
        });

    public Task<Refund> CreateRefundAsync(RefundCreateOptions options) =>
        Task.FromResult(new Refund
        {
            Id = $"re_test_{Guid.NewGuid():N}",
            Amount = options.Amount ?? 0,
            PaymentIntentId = options.PaymentIntent,
            Status = "succeeded",
            Metadata = options.Metadata ?? []
        });

    public Task<TransferReversal> CreateTransferReversalAsync(string transferId, TransferReversalCreateOptions options) =>
        Task.FromResult(new TransferReversal
        {
            Id = $"trr_test_{Guid.NewGuid():N}",
            Amount = options.Amount ?? 0,
            TransferId = transferId
        });
}
