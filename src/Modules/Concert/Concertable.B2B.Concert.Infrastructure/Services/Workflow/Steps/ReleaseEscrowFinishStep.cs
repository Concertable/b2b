using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class ReleaseEscrowFinishStep : IFinishStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IEscrowClient escrowClient;

    public ReleaseEscrowFinishStep(IBookingRepository bookingRepository, IEscrowClient escrowClient)
    {
        this.bookingRepository = bookingRepository;
        this.escrowClient = escrowClient;
    }

    public async Task ExecuteAsync(int concertId)
    {
        var bookingId = await bookingRepository.GetIdByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking not found");

        var release = await escrowClient.ReleaseByBookingIdAsync(bookingId);
        if (release.IsFailed)
            throw new BadRequestException(release.Errors);
    }
}
