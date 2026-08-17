using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class RefundEscrowByApplicationStep : IApplicationCancelStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IApplicationRepository applicationRepository;
    private readonly IBus bus;

    public RefundEscrowByApplicationStep(
        IBookingRepository bookingRepository,
        IApplicationRepository applicationRepository,
        IBus bus)
    {
        this.bookingRepository = bookingRepository;
        this.applicationRepository = applicationRepository;
        this.bus = bus;
    }

    public async Task<UnitResult<CancelApplicationError>> ExecuteAsync(
        int applicationId,
        CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetByApplicationIdAsync(applicationId)
            ?? throw new InvalidOperationException($"Application {applicationId} has no booking.");
        var application = await applicationRepository.GetByIdAsync(applicationId, ct)
            ?? throw new InvalidOperationException($"Application {applicationId} not found.");

        await bus.SendAsync(new RefundEscrowCommand(
            application.BeginCancellation(),
            booking.Id,
            RefundReasonCodes.RequestedByCustomer), ct);
        return new Success();
    }
}
