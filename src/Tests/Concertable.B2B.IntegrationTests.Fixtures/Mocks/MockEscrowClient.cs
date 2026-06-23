using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using FluentResults;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockEscrowClient : IEscrowClient
{
    private readonly MockStripeApiClient stripeApiClient;
    private readonly EscrowStore store;

    public MockEscrowClient(MockStripeApiClient stripeApiClient, EscrowStore store)
    {
        this.stripeApiClient = stripeApiClient;
        this.store = store;
    }

    public async Task<Result<EscrowResponse>> DepositAsync(Guid payerId, Guid payeeId, decimal amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Metadata = new Dictionary<string, string>
            {
                ["type"] = TransactionTypes.Escrow,
                ["bookingId"] = bookingId.ToString()
            }
        });

        return Hold(bookingId, payerId, payeeId, amount, intent.Id);
    }

    public Task<Result<EscrowResponse>> CaptureAsync(Guid payerId, Guid payeeId, decimal amount, string paymentIntentId, int bookingId, CancellationToken ct = default)
    {
        stripeApiClient.UpdateLastMetadata(new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Escrow,
            ["bookingId"] = bookingId.ToString()
        });

        return Task.FromResult(Hold(bookingId, payerId, payeeId, amount, paymentIntentId));
    }

    private Result<EscrowResponse> Hold(int bookingId, Guid payerId, Guid payeeId, decimal amount, string chargeId)
    {
        var escrow = new EscrowRecord(bookingId, payerId, payeeId, (long)(amount * 100), chargeId, EscrowStatus.Held);
        var id = store.Add(escrow);
        return Result.Ok(new EscrowResponse(id, chargeId, EscrowStatus.Held));
    }

    public Task<Result<TransferResponse?>> ReleaseByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Ok<TransferResponse?>(new TransferResponse("tr_mock")));
}
