using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class SetupCheckoutStep : IApplyCheckoutStep
{
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IUserModule userModule;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ITenantContext tenantContext;

    public SetupCheckoutStep(
        IOpportunityRepository opportunityRepository,
        IUserModule userModule,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        ITenantContext tenantContext)
    {
        this.opportunityRepository = opportunityRepository;
        this.userModule = userModule;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.tenantContext = tenantContext;
    }

    public async Task<Checkout> ExecuteAsync(int opportunityId)
    {
        var venueSummary = await opportunityRepository.GetVenueSummaryByIdAsync(opportunityId)
            ?? throw new NotFoundException("Opportunity not found");
        var manager = await userModule.GetManagerByIdAsync(venueSummary.UserId);
        var venue = new PayeeSummary(venueSummary.Name, manager?.Email);
        var contract = (VenueHireContract)contractAccessor.Contract;

        var metadata = new Dictionary<string, string>
        {
            ["type"] = "applicationApply",
            ["opportunityId"] = opportunityId.ToString()
        };

        /* Apply-time checkout — no application snapshot exists yet; the acting user IS the
           artist side, so their tenant comes from the ambient context. */
        var ownerId = tenantContext.TenantId
            ?? throw new ForbiddenException("No tenant for current user");

        var session = await managerPaymentClient.CreateSetupSessionAsync(ownerId, metadata);
        return new Checkout(new FlatPayment(contract.HireFee), venue, session, CheckoutLabels.Charge);
    }
}
