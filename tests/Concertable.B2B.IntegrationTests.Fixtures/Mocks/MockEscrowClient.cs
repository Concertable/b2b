using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Enums;
using Concertable.Testing.Integration;
using FluentResults;
using Stripe;
using Transfer = Concertable.Payment.Contracts.Transfer;
using Refund = Concertable.Payment.Contracts.Refund;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public sealed class MockEscrowClient : IEscrowClient, IResettable
{
    private readonly MockStripeApiClient stripeApiClient;

    /// <summary>The escrow holds B2B initiated, in call order — assert B2B passed the right parties/booking.</summary>
    public List<EscrowHold> Holds { get; } = [];

    /// <summary>Booking ids B2B requested a refund for, in call order — assert cancel refunded the right booking.</summary>
    public List<int> Refunds { get; } = [];

    public MockEscrowClient(MockStripeApiClient stripeApiClient)
    {
        this.stripeApiClient = stripeApiClient;
    }

    public void Reset()
    {
        Holds.Clear();
        Refunds.Clear();
    }

    public async Task<Result<EscrowDeposit>> DepositAsync(Guid payerId, Guid payeeId, decimal amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default)
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
        return Result.Ok(new EscrowDeposit(0, intent.Id, EscrowStatus.Held));
    }

    public Task<Result<EscrowDeposit>> CaptureAsync(Guid payerId, Guid payeeId, decimal amount, string paymentIntentId, int bookingId, CancellationToken ct = default)
    {
        stripeApiClient.UpdateLastMetadata(new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Escrow,
            ["bookingId"] = bookingId.ToString()
        });

        Holds.Add(new EscrowHold(payerId, payeeId, amount, bookingId));
        return Task.FromResult(Result.Ok(new EscrowDeposit(0, paymentIntentId, EscrowStatus.Held)));
    }

    public Task<Result<Transfer?>> ReleaseByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Ok<Transfer?>(new Transfer("tr_mock")));

    public Task<Result<Refund?>> RefundByBookingIdAsync(int bookingId, CancellationToken ct = default)
    {
        Refunds.Add(bookingId);
        return Task.FromResult(Result.Ok<Refund?>(new Refund("re_mock")));
    }
}

public sealed record EscrowHold(Guid PayerId, Guid PayeeId, decimal Amount, int BookingId);
