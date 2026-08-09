using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class RefundEscrowByApplicationStep : IApplicationCancelStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IEscrowOperationsClient escrowClient;

    public RefundEscrowByApplicationStep(IBookingRepository bookingRepository, IEscrowOperationsClient escrowClient)
    {
        this.bookingRepository = bookingRepository;
        this.escrowClient = escrowClient;
    }

    public async Task<UnitResult<CancelApplicationError>> ExecuteAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetByApplicationIdAsync(applicationId)
            ?? throw new InvalidOperationException($"Application {applicationId} has no booking.");

        return (await escrowClient.RefundByBookingIdAsync(booking.Id, ct))
            .MapError(error => (CancelApplicationError)new CancelApplicationError.EscrowRefundFailure(error))
            .Bind(_ => UnitResult.Success<CancelApplicationError>());
    }
}
