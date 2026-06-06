using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PaidAcceptStep : IPaidAcceptStep
{
    private readonly IApplicationValidator applicationValidator;
    private readonly IBookingService bookingService;
    private readonly IContractAccessor contractAccessor;

    public PaidAcceptStep(
        IApplicationValidator applicationValidator,
        IBookingService bookingService,
        IContractAccessor contractAccessor)
    {
        this.applicationValidator = applicationValidator;
        this.bookingService = bookingService;
        this.contractAccessor = contractAccessor;
    }

    public async Task ExecuteAsync(int applicationId, string paymentMethodId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        await bookingService.CreateDeferredAsync(applicationId, contractAccessor.Contract.ContractType, paymentMethodId);
    }
}
