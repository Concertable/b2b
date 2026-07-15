using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class HoldCheckoutStep : IAcceptCheckoutStep
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IDealAccessor dealAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;

    public HoldCheckoutStep(
        IApplicationRepository applicationRepository,
        IDealAccessor dealAccessor,
        IManagerPaymentClient managerPaymentClient)
    {
        this.applicationRepository = applicationRepository;
        this.dealAccessor = dealAccessor;
        this.managerPaymentClient = managerPaymentClient;
    }

    public async Task<Checkout> ExecuteAsync(int applicationId)
    {
        var artist = await applicationRepository.GetArtistPayeeAsync(applicationId)
            .OrNotFound(DisplayNames.Application);
        var venueTenantId = await applicationRepository.GetVenueTenantIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);
        var deal = (FlatFeeDeal)dealAccessor.Deal;

        var metadata = new Dictionary<string, string>
        {
            ["type"] = "applicationAccept",
            ["applicationId"] = applicationId.ToString()
        };

        var session = await managerPaymentClient.CreateHoldSessionAsync(venueTenantId, deal.Fee, metadata);
        return new Checkout(new FlatPayment(deal.Fee), artist, session, CheckoutLabels.Charge);
    }
}
