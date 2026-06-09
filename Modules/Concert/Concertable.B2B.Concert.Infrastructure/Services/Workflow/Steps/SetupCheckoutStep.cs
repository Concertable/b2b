using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class SetupCheckoutStep : IApplyCheckoutStep
{
    private readonly IPayerLookup payerLookup;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ICurrentUser currentUser;
    private readonly ITenantModule tenantModule;

    public SetupCheckoutStep(
        IPayerLookup payerLookup,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        ICurrentUser currentUser,
        ITenantModule tenantModule)
    {
        this.payerLookup = payerLookup;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.currentUser = currentUser;
        this.tenantModule = tenantModule;
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

        var ownerId = await tenantModule.GetTenantIdByUserIdAsync(currentUser.GetId())
            ?? throw new NotFoundException($"No tenant for user {currentUser.GetId()}");

        var session = await managerPaymentClient.CreateSetupSessionAsync(ownerId, metadata);
        return new Checkout(new FlatPayment(contract.HireFee), venue, session, CheckoutLabels.Charge);
    }
}
