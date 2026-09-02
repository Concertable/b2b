using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Specifications;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class RefundEscrowStep : ICancelStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IBus bus;

    public RefundEscrowStep(IBookingRepository bookingRepository, IBus bus)
    {
        this.bookingRepository = bookingRepository;
        this.bus = bus;
    }

    public async Task<UnitResult<CancelConcertError>> ExecuteAsync(int concertId, CancellationToken ct = default)
    {
        var booking = await bookingRepository.GetByConcertIdAsync(concertId, BookingSpecification.CreateWithApplication(), ct)
            ?? throw new InvalidOperationException($"Concert {concertId} has no booking.");
        await bus.SendAsync(new RefundEscrowCommand(
            booking.Application.BeginCancellation(),
            booking.Id,
            RefundReasonCodes.RequestedByCustomer), ct);
        return new Success();
    }
}
