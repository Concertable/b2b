using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using FluentResults;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockEscrowClientFail : IEscrowClient
{
    public Task<Result<EscrowDeposit>> DepositAsync(Guid payerId, Guid payeeId, decimal amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<EscrowDeposit>("Mock escrow deposit failure"));

    public Task<Result<EscrowDeposit>> CaptureAsync(Guid payerId, Guid payeeId, decimal amount, string paymentIntentId, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<EscrowDeposit>("Mock escrow capture failure"));

    public Task<Result<Transfer?>> ReleaseByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<Transfer?>("Mock escrow release failure"));

    public Task<Result<Refund?>> RefundByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<Refund?>("Mock escrow refund failure"));
}
