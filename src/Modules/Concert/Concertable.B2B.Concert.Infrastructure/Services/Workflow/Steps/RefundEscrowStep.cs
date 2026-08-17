using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class RefundEscrowStep : ICancelStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IApplicationRepository applicationRepository;
    private readonly IBus bus;

    public RefundEscrowStep(
        IBookingRepository bookingRepository,
        IApplicationRepository applicationRepository,
        IBus bus)
    {
        this.bookingRepository = bookingRepository;
        this.applicationRepository = applicationRepository;
        this.bus = bus;
    }

    public async Task<UnitResult<CancelConcertError>> ExecuteAsync(int concertId, CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetByConcertIdAsync(concertId, ct)
            ?? throw new InvalidOperationException($"Concert {concertId} has no booking.");
        var application = await applicationRepository.GetByIdAsync(booking.ApplicationId, ct)
            ?? throw new InvalidOperationException($"Booking {booking.Id} has no application.");
        await bus.SendAsync(new RefundEscrowCommand(
            application.BeginCancellation(),
            booking.Id,
            RefundReasonCodes.RequestedByCustomer), ct);
        return new Success();
    }
}
