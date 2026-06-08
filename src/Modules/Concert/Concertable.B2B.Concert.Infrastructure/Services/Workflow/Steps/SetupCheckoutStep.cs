using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class SetupCheckoutStep : IApplyCheckoutStep
{
    private readonly IPayerLookup payerLookup;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ICurrentUser currentUser;

    public SetupCheckoutStep(
        IPayerLookup payerLookup,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        ICurrentUser currentUser)
    {
        this.payerLookup = payerLookup;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.currentUser = currentUser;
    }

    public async Task<Checkout> ExecuteAsync(int opportunityId)
    {
        var venue = await payerLookup.GetVenueByOpportunityIdAsync(opportunityId)
            ?? throw new NotFoundException("Opportunity not found");
        var contract = (VenueHireContract)contractAccessor.Contract;

        var metadata = new Dictionary<string, string>
        {
            ["type"] = "applicationApply",
            ["opportunityId"] = opportunityId.ToString()
        };

        var session = await managerPaymentClient.CreateSetupSessionAsync(currentUser.GetId(), metadata);
        return new Checkout(new FlatPayment(contract.HireFee), venue, session, CheckoutLabels.Charge);
    }
}
