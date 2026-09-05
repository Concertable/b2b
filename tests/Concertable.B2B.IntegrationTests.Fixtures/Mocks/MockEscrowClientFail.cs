using Reunion;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockEscrowClientFail : IEscrowOperationsClient
{
    public Task<Result<EscrowDeposit, EscrowDepositError>> DepositAsync(
        Guid operationId,
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money amount,
        PaymentOperationReference paymentMethod,
        PaymentSession session,
        CancellationToken ct = default) =>
        Task.FromResult(Result<EscrowDeposit, EscrowDepositError>.Failure(
            new EscrowDepositError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<EscrowDeposit, EscrowDepositError>> DepositBoundCommissionAsync(
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money gross,
        PaymentOperationReference paymentMethod,
        PaymentSession session,
        Guid commissionBindingId,
        string externalReference,
        CancellationToken ct = default) =>
        Task.FromResult(Result<EscrowDeposit, EscrowDepositError>.Failure(
            new EscrowDepositError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<EscrowDeposit, EscrowCaptureError>> CaptureAsync(
        Guid operationId,
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money amount,
        PaymentOperationReference authorization,
        CancellationToken ct = default) =>
        Task.FromResult(Result<EscrowDeposit, EscrowCaptureError>.Failure(
            new EscrowCaptureError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<EscrowDeposit, EscrowCaptureError>> CaptureBoundCommissionAsync(
        PaymentOperationReference reference,
        Guid payerId,
        Guid payeeId,
        Money gross,
        PaymentOperationReference authorization,
        Guid commissionBindingId,
        string externalReference,
        CancellationToken ct = default) =>
        Task.FromResult(Result<EscrowDeposit, EscrowCaptureError>.Failure(
            new EscrowCaptureError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<Option<Transfer>, EscrowReleaseOperationError>> ReleaseAsync(
        Guid operationId,
        PaymentOperationReference reference,
        CancellationToken ct = default) =>
        Task.FromResult(Result<Option<Transfer>, EscrowReleaseOperationError>.Failure(
            new EscrowReleaseOperationError.ReleaseFailure(
                new EscrowReleaseError.PaymentFailure(new PaymentError.PaymentRejected()))));

    public Task<Result<Option<Refund>, EscrowRefundError>> RefundAsync(
        Guid operationId,
        PaymentOperationReference reference,
        CancellationToken ct = default) =>
        Task.FromResult(Result<Option<Refund>, EscrowRefundError>.Failure(
            new EscrowRefundError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<Option<Refund>, EscrowRefundError>> RefundBoundCommissionAsync(
        PaymentOperationReference reference,
        Money gross,
        CancellationToken ct = default) =>
        Task.FromResult(Result<Option<Refund>, EscrowRefundError>.Failure(
            new EscrowRefundError.PaymentFailure(new PaymentError.PaymentRejected())));
}
