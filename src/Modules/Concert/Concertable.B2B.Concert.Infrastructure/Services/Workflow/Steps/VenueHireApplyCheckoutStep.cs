using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

/// <summary>
/// Apply-checkout step for <see cref="VenueHireContract"/>. The artist is the payer: this creates a
/// <em>setup session</em> (via <see cref="IManagerPaymentClient.CreateSetupSessionAsync"/>) to save the
/// artist's card for the off-session hold that follows acceptance, rather than charging immediately.
/// </summary>
internal class VenueHireApplyCheckoutStep : IApplyCheckoutStep
{
    private readonly IPayerLookup payerLookup;
    private readonly IContractLoader contractLoader;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ICurrentUser currentUser;

    public VenueHireApplyCheckoutStep(
        IPayerLookup payerLookup,
        IContractLoader contractLoader,
        IManagerPaymentClient managerPaymentClient,
        ICurrentUser currentUser)
    {
        this.payerLookup = payerLookup;
        this.contractLoader = contractLoader;
        this.managerPaymentClient = managerPaymentClient;
        this.currentUser = currentUser;
    }

    public async Task<Checkout> ExecuteAsync(int opportunityId)
    {
        var venue = await payerLookup.GetVenueByOpportunityIdAsync(opportunityId)
            ?? throw new NotFoundException("Opportunity not found");
        var contract = (VenueHireContract)await contractLoader.LoadByOpportunityIdAsync(opportunityId);

        var metadata = new Dictionary<string, string>
        {
            ["type"] = "applicationApply",
            ["opportunityId"] = opportunityId.ToString()
        };

        var session = await managerPaymentClient.CreateSetupSessionAsync(currentUser.GetId(), metadata);
        return new Checkout(new FlatPayment(contract.HireFee), venue, session, CheckoutLabels.Charge);
    }
}
