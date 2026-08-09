using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PaidAcceptStep : IPaidAcceptStep
{
    private readonly IBookingService bookingService;

    public PaidAcceptStep(IBookingService bookingService)
    {
        this.bookingService = bookingService;
    }

    public async Task ExecuteAsync(ApplicationEntity application, string paymentMethodId)
    {
        await bookingService.CreateDeferredAsync(application, paymentMethodId);
    }
}
