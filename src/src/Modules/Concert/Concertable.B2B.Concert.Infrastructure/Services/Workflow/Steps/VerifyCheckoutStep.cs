using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class VerifyCheckoutStep : IAcceptCheckoutStep
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IDealAccessor dealAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly IPaymentAmountMapper paymentAmountMapper;

    public VerifyCheckoutStep(
        IApplicationRepository applicationRepository,
        IDealAccessor dealAccessor,
        IManagerPaymentClient managerPaymentClient,
        IPaymentAmountMapper paymentAmountMapper)
    {
        this.applicationRepository = applicationRepository;
        this.dealAccessor = dealAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.paymentAmountMapper = paymentAmountMapper;
    }

    public async Task<Checkout> ExecuteAsync(int applicationId)
    {
        var artist = await applicationRepository.GetArtistPayeeAsync(applicationId)
            .OrNotFound(DisplayNames.Application);
        /* the user id rides the Stripe metadata so the failure webhook can notify the venue manager */
        var venueManagerId = await applicationRepository.GetVenueManagerIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);
        var venueTenantId = await applicationRepository.GetVenueTenantIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);

        var metadata = new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Verify,
            ["applicationId"] = applicationId.ToString(),
            ["venueManagerId"] = venueManagerId.ToString()
        };

        var session = await managerPaymentClient.CreateVerifySessionAsync(venueTenantId, metadata);
        var amount = paymentAmountMapper.ToPaymentAmount(dealAccessor.Deal);
        return new Checkout(amount, artist, session, CheckoutLabels.Settlement);
    }
}
