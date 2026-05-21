using Concertable.Concert.Application.Workflow.Steps;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class DeferredSettleStep : ISettleStep
{
    private readonly IBookingService bookingService;

    public DeferredSettleStep(IBookingService bookingService)
    {
        this.bookingService = bookingService;
    }

    public Task ExecuteAsync(int bookingId) =>
        bookingService.CompleteAsync(bookingId);
}
