using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Testing.Integration;
using FluentResults;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public sealed class MockEscrowClient : IEscrowClient, IResettable
{
    private readonly MockStripeApiClient stripeApiClient;

    /// <summary>The escrow holds B2B initiated, in call order — assert B2B passed the right parties/booking.</summary>
    public List<EscrowHold> Holds { get; } = [];

    public MockEscrowClient(MockStripeApiClient stripeApiClient)
    {
        this.stripeApiClient = stripeApiClient;
    }

    public void Reset() => Holds.Clear();

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

        Holds.Add(new EscrowHold(payerId, payeeId, amount, bookingId));
        return Result.Ok(new EscrowResponse(0, intent.Id, EscrowStatus.Held));
    }

    public Task<Result<EscrowResponse>> CaptureAsync(Guid payerId, Guid payeeId, decimal amount, string paymentIntentId, int bookingId, CancellationToken ct = default)
    {
        stripeApiClient.UpdateLastMetadata(new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Escrow,
            ["bookingId"] = bookingId.ToString()
        });

        Holds.Add(new EscrowHold(payerId, payeeId, amount, bookingId));
        return Task.FromResult(Result.Ok(new EscrowResponse(0, paymentIntentId, EscrowStatus.Held)));
    }

    public Task<Result<TransferResponse?>> ReleaseByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Ok<TransferResponse?>(new TransferResponse("tr_mock")));
}

public sealed record EscrowHold(Guid PayerId, Guid PayeeId, decimal Amount, int BookingId);
