using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PaidAcceptStep : IPaidAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IDealAccessor contractAccessor;

    public PaidAcceptStep(
        IBookingService bookingService,
        IDealAccessor contractAccessor)
    {
        this.bookingService = bookingService;
        this.contractAccessor = contractAccessor;
    }

    public async Task ExecuteAsync(int applicationId, string paymentMethodId)
    {
        await bookingService.CreateDeferredAsync(applicationId, contractAccessor.Contract.ContractType, paymentMethodId);
    }
}
