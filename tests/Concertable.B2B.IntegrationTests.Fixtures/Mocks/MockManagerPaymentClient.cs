using Reunion;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockManagerPaymentClient : IMockManagerPaymentClient
{
    private readonly MockStripeApiClient stripeApiClient;
    private readonly Dictionary<Guid, PaymentOutcome> settlements = [];
    private readonly SemaphoreSlim settlementSemaphore = new(1, 1);

    public List<(Guid PayerId, Guid PayeeId, decimal Amount, string PaymentMethodId, int BookingId, Guid OperationId)> Payments { get; } = [];

    public MockManagerPaymentClient(MockStripeApiClient stripeApiClient)
    {
        this.stripeApiClient = stripeApiClient;
    }

    public void Reset()
    {
        Payments.Clear();
        settlements.Clear();
    }

    public async Task<Result<PaymentOutcome, ManagerPaymentOperationError>> PayAsync(
        Guid operationId,
        Guid payerId,
        Guid payeeId,
        Money amount,
        string paymentMethodId,
        PaymentSession session,
        int bookingId,
        CancellationToken ct = default)
    {
        await settlementSemaphore.WaitAsync(ct);
        try
        {
            if (settlements.TryGetValue(operationId, out var existing))
                return existing;

            var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
            {
                Amount = amount.ToMinorUnits(),
                Metadata = new Dictionary<string, string>
                {
                    [PaymentMetadataKeys.Type] = TransactionTypes.Settlement,
                    [PaymentMetadataKeys.BookingId] = bookingId.ToString(),
                    [PaymentMetadataKeys.OperationId] = operationId.ToString()
                }
            });
            var outcome = new PaymentOutcome { RequiresAction = false, TransactionId = intent.Id };
            settlements.Add(operationId, outcome);
            Payments.Add((payerId, payeeId, amount.Amount, paymentMethodId, bookingId, operationId));
            return outcome;
        }
        finally
        {
            settlementSemaphore.Release();
        }
    }

    public async Task<Result<PaymentOutcome, ManagerPaymentError>> PayAsync(
        Guid payerId,
        Guid payeeId,
        Money amount,
        string paymentMethodId,
        PaymentSession session,
        int bookingId,
        CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = amount.ToMinorUnits(),
            Metadata = new Dictionary<string, string>
            {
                [PaymentMetadataKeys.Type] = TransactionTypes.Settlement,
                [PaymentMetadataKeys.BookingId] = bookingId.ToString()
            }
        });
        Payments.Add((payerId, payeeId, amount.Amount, paymentMethodId, bookingId, Guid.NewGuid()));
        return new PaymentOutcome { RequiresAction = false, TransactionId = intent.Id };
    }

    public async Task<Result<PaymentOutcome, ManagerPaymentError>> PayBoundCommissionAsync(
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
                [PaymentMetadataKeys.Type] = TransactionTypes.Settlement,
                [PaymentMetadataKeys.BookingId] = bookingId.ToString()
            }
        });
        Payments.Add((payerId, payeeId, gross.Amount, paymentMethodId, bookingId, commissionBindingId));
        return Result<PaymentOutcome, ManagerPaymentError>.Success(new PaymentOutcome { RequiresAction = false, TransactionId = intent.Id });
    }

    public async Task<CheckoutSession> CreateSetupSessionAsync(Guid payerId, IReadOnlyDictionary<string, string> metadata, CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions { Metadata = new Dictionary<string, string>(metadata) });
        return new CheckoutSession(intent.Id + "_secret", "cuss_mock_secret", "cus_mock");
    }

    public async Task<CheckoutSession> CreateVerifySessionAsync(Guid payerId, IReadOnlyDictionary<string, string> metadata, CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreateSetupIntentAsync(new SetupIntentCreateOptions { Metadata = new Dictionary<string, string>(metadata) });
        return new CheckoutSession(intent.Id + "_secret", "cuss_mock_secret", "cus_mock");
    }

    public async Task<CheckoutSession> CreateHoldSessionAsync(Guid payerId, Money amount, IReadOnlyDictionary<string, string> metadata, CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = amount.ToMinorUnits(),
            Metadata = new Dictionary<string, string>(metadata)
        });
        return new CheckoutSession(intent.Id + "_secret", "cuss_mock_secret", "cus_mock");
    }

    public async Task<Result<CheckoutSession, HoldSessionError>> CreateBoundCommissionHoldSessionAsync(
        Guid payerId,
        Money gross,
        IReadOnlyDictionary<string, string> metadata,
        Guid commissionBindingId,
        string externalReference,
        string? stripeSetupIntentId = null,
        CancellationToken ct = default)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = gross.ToMinorUnits(),
            Metadata = new Dictionary<string, string>(metadata)
        });
        return Result<CheckoutSession, HoldSessionError>.Success(new CheckoutSession(intent.Id + "_secret", "cuss_mock_secret", "cus_mock"));
    }

    public Task<string> FindHeldIntentAsync(Guid payerId, int applicationId, CancellationToken ct = default) =>
        Task.FromResult(stripeApiClient.LastPaymentIntentId);

    public Task<Money> GetTicketRevenueAsync(Guid payeeId, DateRange period, CancellationToken ct = default) =>
        Task.FromResult(Money.Gbp(0m));

    public Task<Money> GetSettlementPayoutsAsync(Guid payeeId, DateRange period, CancellationToken ct = default) =>
        Task.FromResult(Money.Gbp(0m));

    public Task<IReadOnlyList<MonthlyPaymentPoint>> GetTicketRevenueByMonthAsync(
        Guid payeeId,
        DateRange period,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MonthlyPaymentPoint>>([]);

    public Task<IReadOnlyList<MonthlyPaymentPoint>> GetSettlementPayoutsByMonthAsync(
        Guid payeeId,
        DateRange period,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MonthlyPaymentPoint>>([]);

    public Task<IReadOnlyList<ManagerSettlement>> GetRecentSettlementsAsync(
        Guid ownerId,
        int take,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ManagerSettlement>>([]);
}
