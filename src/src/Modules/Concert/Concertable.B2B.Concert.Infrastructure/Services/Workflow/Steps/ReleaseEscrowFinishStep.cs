using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class ReleaseEscrowFinishStep : IFinishStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IEscrowOperationsClient escrowClient;

    public ReleaseEscrowFinishStep(IBookingRepository bookingRepository, IEscrowOperationsClient escrowClient)
    {
        this.bookingRepository = bookingRepository;
        this.escrowClient = escrowClient;
    }

    public async Task<UnitResult<FinishConcertError>> ExecuteAsync(int concertId, CancellationToken ct = default)
    {
        var bookingId = await bookingRepository.GetIdByConcertIdAsync(concertId)
            ?? throw new InvalidOperationException($"Concert {concertId} has no booking.");

        return (await escrowClient.ReleaseByBookingIdAsync(bookingId, ct))
            .MapError(error => (FinishConcertError)new FinishConcertError.EscrowReleaseFailure(error))
            .Bind(_ => UnitResult.Success<FinishConcertError>());
    }
}
