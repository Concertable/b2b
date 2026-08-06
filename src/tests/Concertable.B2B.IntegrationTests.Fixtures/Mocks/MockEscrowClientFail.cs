using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using FluentResults;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockEscrowClientFail : IEscrowClient
{
    public Task<Result<EscrowDeposit>> DepositAsync(Guid payerId, Guid payeeId, Money amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<EscrowDeposit>("Mock escrow deposit failure"));

    public Task<Result<EscrowDeposit>> DepositBoundCommissionAsync(
        Guid payerId,
        Guid payeeId,
        long grossMinor,
        Currency currency,
        string paymentMethodId,
        PaymentSession session,
        int bookingId,
        Guid commissionBindingId,
        string externalReference,
        long expectedCommissionMinor,
        long expectedPayerTotalMinor,
        string? stripeSetupIntentId = null,
        CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<EscrowDeposit>("Mock escrow deposit failure"));

    public Task<Result<EscrowDeposit>> CaptureAsync(Guid payerId, Guid payeeId, Money amount, string paymentIntentId, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<EscrowDeposit>("Mock escrow capture failure"));

    public Task<Result<EscrowDeposit>> CaptureBoundCommissionAsync(
        Guid payerId,
        Guid payeeId,
        long grossMinor,
        Currency currency,
        string paymentIntentId,
        int bookingId,
        Guid commissionBindingId,
        string externalReference,
        long expectedCommissionMinor,
        long expectedPayerTotalMinor,
        CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<EscrowDeposit>("Mock escrow capture failure"));

    public Task<Result<Transfer?>> ReleaseByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<Transfer?>("Mock escrow release failure"));

    public Task<Result<Refund?>> RefundByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<Refund?>("Mock escrow refund failure"));

    public Task<Result<Refund?>> RefundBoundCommissionByBookingIdAsync(
        int bookingId,
        long grossMinor,
        Currency currency,
        CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<Refund?>("Mock escrow refund failure"));
}
