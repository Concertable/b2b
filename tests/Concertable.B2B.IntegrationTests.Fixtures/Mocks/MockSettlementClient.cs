using Reunion;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockSettlementClient : IMockSettlementClient
{
    private readonly Dictionary<Guid, PaymentOutcome> settlements = [];
    private readonly SemaphoreSlim settlementSemaphore = new(1, 1);
    private readonly MockPaymentOperations paymentOperations;

    public MockSettlementClient(MockPaymentOperations paymentOperations)
    {
        this.paymentOperations = paymentOperations;
    }

    public List<(Guid PayerId, Guid PayeeId, decimal Amount, PaymentOperationReference PaymentMethod, PaymentOperationReference Reference, Guid OperationId)> Payments { get; } = [];

    public void Reset()
    {
        Payments.Clear();
        settlements.Clear();
    }

    public async Task<Result<PaymentOutcome, PaymentMethodChargeError>> PayAsync(
        Guid operationId,
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money amount,
        PaymentOperationReference paymentMethod,
        PaymentSession session,
        CancellationToken ct = default)
    {
        await settlementSemaphore.WaitAsync(ct);
        try
        {
            if (settlements.TryGetValue(operationId, out var existing))
                return existing;

            var outcome = new PaymentOutcome { RequiresAction = false };
            settlements.Add(operationId, outcome);
            Payments.Add((
                payerId,
                payeeId,
                amount.Amount,
                paymentMethod,
                reference,
                operationId));
            paymentOperations.Record(reference, operationId);
            return outcome;
        }
        finally
        {
            settlementSemaphore.Release();
        }
    }

    public Task<Result<PaymentOutcome, PaymentMethodChargeError>> PayBoundCommissionAsync(
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money gross,
        PaymentOperationReference paymentMethod,
        PaymentSession session,
        Guid commissionBindingId,
        string externalReference,
        CancellationToken ct = default)
    {
        Payments.Add((
            payerId,
            payeeId,
            gross.Amount,
            paymentMethod,
            reference,
            commissionBindingId));
        return Task.FromResult(
            Result<PaymentOutcome, PaymentMethodChargeError>.Success(new PaymentOutcome { RequiresAction = false }));
    }

    public Task<Result<Option<Refund>, SettlementRefundError>> RefundBoundCommissionAsync(
        PaymentOperationReference reference,
        Money gross,
        string? reason = null,
        CancellationToken ct = default) =>
        Task.FromResult(
            Result<Option<Refund>, SettlementRefundError>.Success(Option.Some(new Refund(Guid.NewGuid()))));

    public Task<Money> GetPaymentRevenueAsync(Guid payeeId, DateRange period, CancellationToken ct = default) =>
        Task.FromResult(Money.Gbp(0m));

    public Task<Money> GetSettlementPayoutsAsync(Guid payeeId, DateRange period, CancellationToken ct = default) =>
        Task.FromResult(Money.Gbp(0m));

    public Task<IReadOnlyList<MonthlyPaymentPoint>> GetPaymentRevenueByMonthAsync(
        Guid payeeId,
        DateRange period,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MonthlyPaymentPoint>>([]);

    public Task<IReadOnlyList<MonthlyPaymentPoint>> GetSettlementPayoutsByMonthAsync(
        Guid payeeId,
        DateRange period,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MonthlyPaymentPoint>>([]);

    public Task<IReadOnlyList<PaymentSettlement>> GetRecentSettlementsAsync(
        Guid ownerId,
        int take,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PaymentSettlement>>([]);
}
