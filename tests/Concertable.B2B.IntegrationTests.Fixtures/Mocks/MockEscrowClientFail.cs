using Reunion;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockEscrowClientFail : IEscrowOperationsClient
{
    public Task<Result<EscrowDeposit, EscrowDepositError>> DepositAsync(Guid payerId, Guid payeeId, Money amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result<EscrowDeposit, EscrowDepositError>.Failure(new EscrowDepositError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<EscrowDeposit, EscrowDepositError>> DepositBoundCommissionAsync(
        Guid payerId,
        Guid payeeId,
        Money gross,
        string paymentMethodId,
        PaymentSession session,
        int bookingId,
        Guid commissionBindingId,
        string externalReference,
        string? stripeSetupIntentId = null,
        CancellationToken ct = default) =>
        Task.FromResult(Result<EscrowDeposit, EscrowDepositError>.Failure(new EscrowDepositError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<EscrowDeposit, EscrowCaptureError>> CaptureAsync(Guid payerId, Guid payeeId, Money amount, string paymentIntentId, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result<EscrowDeposit, EscrowCaptureError>.Failure(new EscrowCaptureError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<EscrowDeposit, EscrowCaptureError>> CaptureBoundCommissionAsync(
        Guid payerId,
        Guid payeeId,
        Money gross,
        string paymentIntentId,
        int bookingId,
        Guid commissionBindingId,
        string externalReference,
        CancellationToken ct = default) =>
        Task.FromResult(Result<EscrowDeposit, EscrowCaptureError>.Failure(new EscrowCaptureError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<Option<Transfer>, EscrowReleaseOperationError>> ReleaseByBookingIdAsync(
        Guid operationId,
        int bookingId,
        CancellationToken ct = default) =>
        Task.FromResult(Result<Option<Transfer>, EscrowReleaseOperationError>.Failure(
            new EscrowReleaseOperationError.ReleaseFailure(
                new EscrowReleaseError.PaymentFailure(new PaymentError.PaymentRejected()))));

    public Task<Result<Option<Transfer>, EscrowReleaseError>> ReleaseByBookingIdAsync(
        int bookingId,
        CancellationToken ct = default) =>
        Task.FromResult(Result<Option<Transfer>, EscrowReleaseError>.Failure(
            new EscrowReleaseError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<Option<Transfer>, EscrowReleaseOperationError>> ReleaseByBookingIdAsync(
        Guid operationId,
        int bookingId,
        CancellationToken ct = default) =>
        Task.FromResult(Result<Option<Transfer>, EscrowReleaseOperationError>.Failure(
            new EscrowReleaseOperationError.ReleaseFailure(
                new EscrowReleaseError.PaymentFailure(new PaymentError.PaymentRejected()))));

    public Task<Result<Option<Refund>, EscrowRefundError>> RefundByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result<Option<Refund>, EscrowRefundError>.Failure(new EscrowRefundError.PaymentFailure(new PaymentError.PaymentRejected())));

    public Task<Result<Option<Refund>, EscrowRefundError>> RefundBoundCommissionByBookingIdAsync(
        int bookingId,
        Money gross,
        CancellationToken ct = default) =>
        Task.FromResult(Result<Option<Refund>, EscrowRefundError>.Failure(new EscrowRefundError.PaymentFailure(new PaymentError.PaymentRejected())));
}
