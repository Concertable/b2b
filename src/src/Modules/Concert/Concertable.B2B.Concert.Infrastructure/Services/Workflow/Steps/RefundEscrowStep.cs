using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class RefundEscrowStep : ICancelStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IEscrowOperationsClient escrowClient;

    public RefundEscrowStep(IBookingRepository bookingRepository, IEscrowOperationsClient escrowClient)
    {
        this.bookingRepository = bookingRepository;
        this.escrowClient = escrowClient;
    }

    public async Task<UnitResult<CancelConcertError>> ExecuteAsync(int concertId, CancellationToken ct = default)
    {
        var bookingId = await bookingRepository.GetIdByConcertIdAsync(concertId)
            ?? throw new InvalidOperationException($"Concert {concertId} has no booking.");

        return (await escrowClient.RefundByBookingIdAsync(bookingId, ct))
            .MapError(error => (CancelConcertError)new CancelConcertError.EscrowRefundFailure(error))
            .Bind(_ => UnitResult.Success<CancelConcertError>());
    }
}
