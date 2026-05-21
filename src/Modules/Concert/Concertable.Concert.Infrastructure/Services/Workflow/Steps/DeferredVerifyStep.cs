using Concertable.Concert.Application.Workflow.Steps;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class DeferredVerifyStep : IVerifyStep
{
    private readonly IBookingRepository bookingRepository;
    private readonly IConcertDraftService concertDraftService;

    public DeferredVerifyStep(IBookingRepository bookingRepository, IConcertDraftService concertDraftService)
    {
        this.bookingRepository = bookingRepository;
        this.concertDraftService = concertDraftService;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        var booking = await bookingRepository.GetByApplicationIdAsync(applicationId)
            ?? throw new NotFoundException("Booking not found for application");

        var result = await concertDraftService.CreateAsync(booking.Id);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);
    }
}
