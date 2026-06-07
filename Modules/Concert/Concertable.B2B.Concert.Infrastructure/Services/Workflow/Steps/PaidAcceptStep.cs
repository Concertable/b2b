using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PaidAcceptStep : IPaidAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IContractAccessor contractAccessor;

    public PaidAcceptStep(
        IBookingService bookingService,
        IContractAccessor contractAccessor)
    {
        this.bookingService = bookingService;
        this.contractAccessor = contractAccessor;
    }

    public async Task ExecuteAsync(int applicationId, string paymentMethodId)
    {
        await bookingService.CreateDeferredAsync(applicationId, contractAccessor.Contract.ContractType, paymentMethodId);
    }
}
