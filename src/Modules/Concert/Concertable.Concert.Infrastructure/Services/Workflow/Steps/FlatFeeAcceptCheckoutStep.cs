using Concertable.Concert.Application.Responses;
using Concertable.Concert.Application.Workflow.Steps;
using Concertable.Contract.Contracts;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class FlatFeeAcceptCheckoutStep : IAcceptCheckoutStep
{
    private readonly IPayerLookup payerLookup;
    private readonly IContractLoader contractLoader;
    private readonly IManagerPaymentClient managerPaymentClient;

    public FlatFeeAcceptCheckoutStep(
        IPayerLookup payerLookup,
        IContractLoader contractLoader,
        IManagerPaymentClient managerPaymentClient)
    {
        this.payerLookup = payerLookup;
        this.contractLoader = contractLoader;
        this.managerPaymentClient = managerPaymentClient;
    }

    public async Task<Checkout> ExecuteAsync(int applicationId)
    {
        var artist = await payerLookup.GetArtistAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        var venueManagerId = await payerLookup.GetVenueManagerIdAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        var contract = (FlatFeeContract)await contractLoader.LoadByApplicationIdAsync(applicationId);

        var metadata = new Dictionary<string, string>
        {
            ["type"] = "applicationAccept",
            ["applicationId"] = applicationId.ToString()
        };

        var session = await managerPaymentClient.CreateHoldSessionAsync(venueManagerId, contract.Fee, metadata);
        return new Checkout(new FlatPayment(contract.Fee), artist, session, CheckoutLabels.Charge);
    }
}
