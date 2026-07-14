using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class HoldCheckoutStep : IAcceptCheckoutStep
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IDealAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;

    public HoldCheckoutStep(
        IApplicationRepository applicationRepository,
        IDealAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient)
    {
        this.applicationRepository = applicationRepository;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
    }

    public async Task<Checkout> ExecuteAsync(int applicationId)
    {
        var artist = await applicationRepository.GetArtistPayeeAsync(applicationId)
            .OrNotFound("Application");
        var venueTenantId = await applicationRepository.GetVenueTenantIdAsync(applicationId)
            .OrNotFound("Application");
        var contract = (FlatFeeDeal)contractAccessor.Contract;

        var metadata = new Dictionary<string, string>
        {
            ["type"] = "applicationAccept",
            ["applicationId"] = applicationId.ToString()
        };

        var session = await managerPaymentClient.CreateHoldSessionAsync(venueTenantId, contract.Fee, metadata);
        return new Checkout(new FlatPayment(contract.Fee), artist, session, CheckoutLabels.Charge);
    }
}
