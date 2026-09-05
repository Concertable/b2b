using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;
using Concertable.Payment.Contracts.Enums;
using Concertable.Testing.Integration;
using Reunion;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

public sealed class MockEscrowClient : IEscrowOperationsClient, IResettable
{
    private readonly HashSet<Guid> releaseOperations = [];

    /// <summary>The escrow holds B2B initiated, in call order — assert B2B passed the right parties/reference.</summary>
    public List<EscrowHold> Holds { get; } = [];

    /// <summary>The escrows B2B requested a refund for, in call order.</summary>
    public List<PaymentOperationReference> Refunds { get; } = [];

    public List<(PaymentOperationReference Reference, Guid OperationId)> Releases { get; } = [];

    public void Reset()
    {
        Holds.Clear();
        Refunds.Clear();
        Releases.Clear();
        releaseOperations.Clear();
    }

    public Task<Result<EscrowDeposit, EscrowDepositError>> DepositAsync(
        Guid operationId,
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money amount,
        PaymentOperationReference paymentMethod,
        PaymentSession session,
        CancellationToken ct = default)
    {
        Holds.Add(new EscrowHold(payerId, payeeId, amount.Amount, reference));
        return Task.FromResult(
            Result<EscrowDeposit, EscrowDepositError>.Success(new EscrowDeposit(0, EscrowStatus.Held)));
    }

    public Task<Result<EscrowDeposit, EscrowDepositError>> DepositBoundCommissionAsync(
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
        Holds.Add(new EscrowHold(payerId, payeeId, gross.Amount, reference));
        return Task.FromResult(
            Result<EscrowDeposit, EscrowDepositError>.Success(new EscrowDeposit(0, EscrowStatus.Held)));
    }

    public Task<Result<EscrowDeposit, EscrowCaptureError>> CaptureAsync(
        Guid operationId,
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money amount,
        PaymentOperationReference authorization,
        CancellationToken ct = default)
    {
        Holds.Add(new EscrowHold(payerId, payeeId, amount.Amount, reference));
        return Task.FromResult(
            Result<EscrowDeposit, EscrowCaptureError>.Success(new EscrowDeposit(0, EscrowStatus.Held)));
    }

    public Task<Result<EscrowDeposit, EscrowCaptureError>> CaptureBoundCommissionAsync(
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money gross,
        PaymentOperationReference authorization,
        Guid commissionBindingId,
        string externalReference,
        CancellationToken ct = default)
    {
        Holds.Add(new EscrowHold(payerId, payeeId, gross.Amount, reference));
        return Task.FromResult(
            Result<EscrowDeposit, EscrowCaptureError>.Success(new EscrowDeposit(0, EscrowStatus.Held)));
    }

    public Task<Result<Option<Transfer>, EscrowReleaseOperationError>> ReleaseAsync(
        Guid operationId,
        PaymentOperationReference reference,
        CancellationToken ct = default)
    {
        lock (releaseOperations)
        {
            if (releaseOperations.Add(operationId))
                Releases.Add((reference, operationId));
        }
        return Task.FromResult(
            Result<Option<Transfer>, EscrowReleaseOperationError>.Success(
                Option.Some(new Transfer(operationId))));
    }

    public Task<Result<Option<Refund>, EscrowRefundError>> RefundAsync(
        Guid operationId,
        PaymentOperationReference reference,
        CancellationToken ct = default)
    {
        Refunds.Add(reference);
        return Task.FromResult(
            Result<Option<Refund>, EscrowRefundError>.Success(Option.Some(new Refund(operationId))));
    }

    public Task<Result<Option<Refund>, EscrowRefundError>> RefundBoundCommissionAsync(
        PaymentOperationReference reference,
        Money gross,
        CancellationToken ct = default)
    {
        Refunds.Add(reference);
        return Task.FromResult(
            Result<Option<Refund>, EscrowRefundError>.Success(Option.Some(new Refund(Guid.NewGuid()))));
    }
}

public sealed record EscrowHold(
    Guid PayerId,
    Guid PayeeId,
    decimal Amount,
    PaymentOperationReference Reference);
