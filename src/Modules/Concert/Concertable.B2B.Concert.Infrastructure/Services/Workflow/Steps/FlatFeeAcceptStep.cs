using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class FlatFeeAcceptStep : ISimpleAcceptStep
{
    private readonly IApplicationValidator applicationValidator;
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;
    private readonly IPayerLookup payerLookup;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ILogger<FlatFeeAcceptStep> logger;

    public FlatFeeAcceptStep(
        IApplicationValidator applicationValidator,
        IBookingService bookingService,
        IEscrowClient escrowClient,
        IPayerLookup payerLookup,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        ILogger<FlatFeeAcceptStep> logger)
    {
        this.applicationValidator = applicationValidator;
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.payerLookup = payerLookup;
        this.contractAccessor = contractAccessor;
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
        var contract = (FlatFeeContract)contractAccessor.Contract;
        var booking = await bookingService.CreateStandardAsync(applicationId, contract.ContractType);

        var paymentIntentId = await managerPaymentClient.FindHeldIntentAsync(venueManagerId, applicationId);

        logger.AcceptingFlatFeeApplication(applicationId, booking.Id, paymentIntentId, contract.Fee, "GBP", venueManagerId, artistManagerId);

        var bind = await escrowClient.CaptureAsync(venueManagerId, artistManagerId, contract.Fee, paymentIntentId, booking.Id);
        if (bind.IsFailed)
            throw new BadRequestException(bind.Errors);
    }
}
