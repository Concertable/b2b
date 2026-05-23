using Concertable.Concert.Application.Responses;
using Concertable.Concert.Application.Workflow.Steps;
using Concertable.Contract.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class DoorSplitAcceptCheckoutStep : IAcceptCheckoutStep
{
    private readonly IPayerLookup payerLookup;
    private readonly IContractLoader contractLoader;
    private readonly IManagerPaymentClient managerPaymentClient;

    public DoorSplitAcceptCheckoutStep(
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
        var contract = (DoorSplitContract)await contractLoader.LoadByApplicationIdAsync(applicationId);

        var metadata = new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Verify,
            ["applicationId"] = applicationId.ToString(),
            ["venueManagerId"] = venueManagerId.ToString()
        };

        var session = await managerPaymentClient.CreateVerifySessionAsync(venueManagerId, metadata);
        return new Checkout(new DoorSharePayment(contract.ArtistDoorPercent), artist, session, CheckoutLabels.Settlement);
    }
}
