using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class VenueHireFinishStep : IFinishStep
{
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;

    public VenueHireFinishStep(IBookingService bookingService, IEscrowClient escrowClient)
    {
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
    }

    public async Task ExecuteAsync(int concertId)
    {
        var booking = await bookingService.CompleteByConcertIdAsync(concertId);

        var release = await escrowClient.ReleaseByBookingIdAsync(booking.Id);
        if (release.IsFailed)
            throw new BadRequestException(release.Errors);
    }
}
