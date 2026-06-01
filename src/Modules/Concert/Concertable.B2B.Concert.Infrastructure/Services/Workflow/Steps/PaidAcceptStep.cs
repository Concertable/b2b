using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PaidAcceptStep : IPaidAcceptStep
{
    private readonly IApplicationValidator applicationValidator;
    private readonly IBookingService bookingService;

    public PaidAcceptStep(IApplicationValidator applicationValidator, IBookingService bookingService)
    {
        this.applicationValidator = applicationValidator;
        this.bookingService = bookingService;
    }

    public async Task ExecuteAsync(int applicationId, string paymentMethodId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        await bookingService.CreateDeferredAsync(applicationId, paymentMethodId);
    }
}
