using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PaidAcceptStep : IPaidAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IDealAccessor dealAccessor;

    public PaidAcceptStep(
        IBookingService bookingService,
        IDealAccessor dealAccessor)
    {
        this.bookingService = bookingService;
        this.dealAccessor = dealAccessor;
    }

    public async Task ExecuteAsync(int applicationId, string paymentMethodId)
    {
        await bookingService.CreateDeferredAsync(applicationId, dealAccessor.Deal.DealType, paymentMethodId);
    }
}
