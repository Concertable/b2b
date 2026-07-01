using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class HoldCheckoutStep : IAcceptCheckoutStep
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;

    public HoldCheckoutStep(
        IApplicationRepository applicationRepository,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient)
    {
        this.applicationRepository = applicationRepository;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
    }

    public async Task<Checkout> ExecuteAsync(int applicationId)
    {
        var artist = await applicationRepository.GetArtistPayeeAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        var venueTenantId = await applicationRepository.GetVenueTenantIdAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        var contract = (FlatFeeContract)contractAccessor.Contract;

        var metadata = new Dictionary<string, string>
        {
            ["type"] = "applicationAccept",
            ["applicationId"] = applicationId.ToString()
        };

        var session = await managerPaymentClient.CreateHoldSessionAsync(venueTenantId, contract.Fee, metadata);
        return new Checkout(new FlatPayment(contract.Fee), artist, session, CheckoutLabels.Charge);
    }
}
