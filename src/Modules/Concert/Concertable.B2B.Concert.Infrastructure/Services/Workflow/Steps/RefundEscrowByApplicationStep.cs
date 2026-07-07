using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class RefundEscrowByApplicationStep : IApplicationCancelStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IEscrowClient escrowClient;

    public RefundEscrowByApplicationStep(IBookingRepository bookingRepository, IEscrowClient escrowClient)
    {
        this.bookingRepository = bookingRepository;
        this.escrowClient = escrowClient;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        var booking = await bookingRepository.GetByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException("Booking not found for application");

        var refund = await escrowClient.RefundByBookingIdAsync(booking.Id);
        if (refund.IsFailed)
            throw new BadRequestException(refund.Errors);
    }
}
