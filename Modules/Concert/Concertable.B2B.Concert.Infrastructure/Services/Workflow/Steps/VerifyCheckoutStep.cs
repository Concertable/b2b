using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class VerifyCheckoutStep : IAcceptCheckoutStep
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly IPaymentAmountMapper paymentAmountMapper;

    public VerifyCheckoutStep(
        IApplicationRepository applicationRepository,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        IPaymentAmountMapper paymentAmountMapper)
    {
        this.applicationRepository = applicationRepository;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.paymentAmountMapper = paymentAmountMapper;
    }

    public async Task<Checkout> ExecuteAsync(int applicationId)
    {
        var artist = await applicationRepository.GetArtistPayeeAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        /* the user id rides the Stripe metadata so the failure webhook can notify the venue manager */
        var venueManagerId = await applicationRepository.GetVenueManagerIdAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        var venueTenantId = await applicationRepository.GetVenueTenantIdAsync(applicationId)
            ?? throw new NotFoundException("Application not found");

        var metadata = new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Verify,
            ["applicationId"] = applicationId.ToString(),
            ["venueManagerId"] = venueManagerId.ToString()
        };

        var session = await managerPaymentClient.CreateVerifySessionAsync(venueTenantId, metadata);
        var amount = paymentAmountMapper.ToPaymentAmount(contractAccessor.Contract);
        return new Checkout(amount, artist, session, CheckoutLabels.Settlement);
    }
}
