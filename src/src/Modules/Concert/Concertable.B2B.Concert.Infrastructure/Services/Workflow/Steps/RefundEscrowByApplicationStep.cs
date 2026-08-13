using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class RefundEscrowByApplicationStep : IApplicationCancelStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IBus bus;

    public RefundEscrowByApplicationStep(IBookingRepository bookingRepository, IBus bus)
    {
        this.bookingRepository = bookingRepository;
        this.bus = bus;
    }

    public async Task<UnitResult<CancelApplicationError>> ExecuteAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetByApplicationIdAsync(applicationId)
            ?? throw new InvalidOperationException($"Application {applicationId} has no booking.");

        await bus.SendAsync(new RefundEscrowCommand(
            booking.Application.BeginCancellation(),
            booking.Id,
            "application-cancelled"), ct);
        return new Success();
    }
}
