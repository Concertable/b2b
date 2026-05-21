using Concertable.Concert.Application.Workflow.Steps;
using Concertable.Contract.Contracts;
using Concertable.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class FlatFeeAcceptStep : ISimpleAcceptStep
{
    private readonly IApplicationValidator applicationValidator;
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;
    private readonly IPayerLookup payerLookup;
    private readonly IContractLoader contractLoader;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ILogger<FlatFeeAcceptStep> logger;

    public FlatFeeAcceptStep(
        IApplicationValidator applicationValidator,
        IBookingService bookingService,
        IEscrowClient escrowClient,
        IPayerLookup payerLookup,
        IContractLoader contractLoader,
        IManagerPaymentClient managerPaymentClient,
        ILogger<FlatFeeAcceptStep> logger)
    {
        this.applicationValidator = applicationValidator;
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.payerLookup = payerLookup;
        this.contractLoader = contractLoader;
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        var (venueManagerId, artistManagerId) = await payerLookup.GetManagerIdsAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        var contract = (FlatFeeContract)await contractLoader.LoadByApplicationIdAsync(applicationId);
        var booking = await bookingService.CreateStandardAsync(applicationId);

        var paymentIntentId = await managerPaymentClient.FindHeldIntentAsync(venueManagerId, applicationId);

        logger.LogInformation(
            "Accepting application {ApplicationId} (booking {BookingId}): binding pre-authorised PaymentIntent {PaymentIntentId} for {Amount} {Currency} from {PayerId} on behalf of {PayeeId}",
            applicationId, booking.Id, paymentIntentId, contract.Fee, "GBP", venueManagerId, artistManagerId);

        var bind = await escrowClient.CaptureAsync(venueManagerId, artistManagerId, contract.Fee, paymentIntentId, booking.Id);
        if (bind.IsFailed)
            throw new BadRequestException(bind.Errors);
    }
}
