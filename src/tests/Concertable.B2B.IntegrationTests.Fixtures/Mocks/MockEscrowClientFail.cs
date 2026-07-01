using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using FluentResults;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal sealed class MockEscrowClientFail : IEscrowClient
{
    public Task<Result<EscrowResponse>> DepositAsync(Guid payerId, Guid payeeId, decimal amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<EscrowResponse>("Mock escrow deposit failure"));

    public Task<Result<EscrowResponse>> CaptureAsync(Guid payerId, Guid payeeId, decimal amount, string paymentIntentId, int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<EscrowResponse>("Mock escrow capture failure"));

    public Task<Result<TransferResponse?>> ReleaseByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Fail<TransferResponse?>("Mock escrow release failure"));
}
