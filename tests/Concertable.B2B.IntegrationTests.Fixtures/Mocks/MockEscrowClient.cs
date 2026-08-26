using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;
using Concertable.Payment.Contracts.Enums;
using Concertable.Testing.Integration;
using Reunion;
using Stripe;
using Transfer = Concertable.Payment.Contracts.Transfer;
using Refund = Concertable.Payment.Contracts.Refund;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public sealed class MockEscrowClient : IEscrowOperationsClient, IResettable
{
    private readonly MockStripeApiClient stripeApiClient;
    private readonly HashSet<Guid> releaseOperations = [];

    /// <summary>The escrow holds B2B initiated, in call order — assert B2B passed the right parties/booking.</summary>
    public List<EscrowHold> Holds { get; } = [];

    /// <summary>Booking ids B2B requested a refund for, in call order — assert cancel refunded the right booking.</summary>
    public List<int> Refunds { get; } = [];
    public List<(int BookingId, Guid OperationId)> Releases { get; } = [];

    public MockEscrowClient(MockStripeApiClient stripeApiClient)
    {
        this.stripeApiClient = stripeApiClient;
    }

    public void Reset()
    {
        Holds.Clear();
        Refunds.Clear();
        Releases.Clear();
        releaseOperations.Clear();
    }

    public async Task<Result<EscrowDeposit, EscrowDepositError>> DepositAsync(Guid payerId, Guid payeeId, Money amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = amount.ToMinorUnits(),
            Metadata = new Dictionary<string, string>
            {
                [PaymentMetadataKeys.Type] = TransactionTypes.Escrow,
                [PaymentMetadataKeys.BookingId] = bookingId.ToString()
            }
        });

        Holds.Add(new EscrowHold(payerId, payeeId, amount.Amount, bookingId));
        return Result<EscrowDeposit, EscrowDepositError>.Success(new EscrowDeposit(0, intent.Id, EscrowStatus.Held));
    }

    public async Task<Result<EscrowDeposit, EscrowDepositError>> DepositBoundCommissionAsync(
        Guid payerId,
        Guid payeeId,
        Money gross,
        string paymentMethodId,
        PaymentSession session,
        int bookingId,
        Guid commissionBindingId,
        string externalReference,
        string? stripeSetupIntentId = null,
        CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = gross.ToMinorUnits(),
            Metadata = new Dictionary<string, string>
            {
                [PaymentMetadataKeys.Type] = TransactionTypes.Escrow,
                [PaymentMetadataKeys.BookingId] = bookingId.ToString()
            }
        });

        Holds.Add(new EscrowHold(payerId, payeeId, gross.Amount, bookingId));
        return Result<EscrowDeposit, EscrowDepositError>.Success(new EscrowDeposit(0, intent.Id, EscrowStatus.Held));
    }

    public Task<Result<EscrowDeposit, EscrowCaptureError>> CaptureAsync(Guid payerId, Guid payeeId, Money amount, string paymentIntentId, int bookingId, CancellationToken ct = default)
    {
        stripeApiClient.UpdateLastMetadata(new Dictionary<string, string>
        {
            [PaymentMetadataKeys.Type] = TransactionTypes.Escrow,
            [PaymentMetadataKeys.BookingId] = bookingId.ToString()
        });

        Holds.Add(new EscrowHold(payerId, payeeId, amount.Amount, bookingId));
        return Task.FromResult(Result<EscrowDeposit, EscrowCaptureError>.Success(new EscrowDeposit(0, paymentIntentId, EscrowStatus.Held)));
    }

    public Task<Result<EscrowDeposit, EscrowCaptureError>> CaptureBoundCommissionAsync(
        Guid payerId,
        Guid payeeId,
        Money gross,
        string paymentIntentId,
        int bookingId,
        Guid commissionBindingId,
        string externalReference,
        CancellationToken ct = default)
    {
        stripeApiClient.UpdateLastMetadata(new Dictionary<string, string>
        {
            [PaymentMetadataKeys.Type] = TransactionTypes.Escrow,
            [PaymentMetadataKeys.BookingId] = bookingId.ToString()
        });

        Holds.Add(new EscrowHold(payerId, payeeId, gross.Amount, bookingId));
        return Task.FromResult(Result<EscrowDeposit, EscrowCaptureError>.Success(new EscrowDeposit(0, paymentIntentId, EscrowStatus.Held)));
    }

    public Task<Result<Option<Transfer>, EscrowReleaseOperationError>> ReleaseByBookingIdAsync(
        Guid operationId,
        int bookingId,
        CancellationToken ct = default)
    {
        lock (releaseOperations)
        {
            if (releaseOperations.Add(operationId))
                Releases.Add((bookingId, operationId));
        }
        return Task.FromResult(
            Result<Option<Transfer>, EscrowReleaseOperationError>.Success(
                Option.Some(new Transfer("tr_mock"))));
    }

    public Task<Result<Option<Transfer>, EscrowReleaseError>> ReleaseByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        Task.FromResult(
            Result<Option<Transfer>, EscrowReleaseError>.Success(
                Option.Some(new Transfer("tr_mock"))));

    public Task<Result<Option<Refund>, EscrowRefundError>> RefundByBookingIdAsync(int bookingId, CancellationToken ct = default)
    {
        Refunds.Add(bookingId);
        return Task.FromResult(Result<Option<Refund>, EscrowRefundError>.Success(Option.Some(new Refund("re_mock"))));
    }

    public Task<Result<Option<Refund>, EscrowRefundError>> RefundBoundCommissionByBookingIdAsync(
        int bookingId,
        Money gross,
        CancellationToken ct = default) =>
        RefundByBookingIdAsync(bookingId, ct);
}

public sealed record EscrowHold(Guid PayerId, Guid PayeeId, decimal Amount, int BookingId);
