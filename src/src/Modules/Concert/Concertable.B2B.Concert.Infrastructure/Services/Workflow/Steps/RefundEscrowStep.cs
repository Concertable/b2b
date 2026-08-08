using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

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

    public async Task ExecuteAsync(int concertId)
    {
        var bookingId = await bookingRepository.GetIdByConcertIdAsync(concertId)
            .OrNotFound(DisplayNames.Booking);

        var refund = await escrowClient.RefundByBookingIdAsync(bookingId);
        if (refund.TryGetError(out var error))
            throw new BadRequestException(error.Definition.Message);
    }
}
