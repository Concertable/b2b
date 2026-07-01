using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class CaptureEscrowAcceptStep : ISimpleAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;
    private readonly IApplicationRepository applicationRepository;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ILogger<CaptureEscrowAcceptStep> logger;

    public CaptureEscrowAcceptStep(
        IBookingService bookingService,
        IEscrowClient escrowClient,
        IApplicationRepository applicationRepository,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        ILogger<CaptureEscrowAcceptStep> logger)
    {
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.applicationRepository = applicationRepository;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        /* FlatFee: the venue tenant pays the artist tenant, per the application's frozen snapshot. */
        var (venueTenantId, artistTenantId) = await applicationRepository.GetTenantPairAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        var contract = (FlatFeeContract)contractAccessor.Contract;
        var booking = await bookingService.CreateStandardAsync(applicationId, contract.ContractType);

        var paymentIntentId = await managerPaymentClient.FindHeldIntentAsync(venueTenantId, applicationId);

        logger.AcceptingFlatFeeApplication(applicationId, booking.Id, paymentIntentId, contract.Fee, "GBP", venueTenantId, artistTenantId);

        var bind = await escrowClient.CaptureAsync(venueTenantId, artistTenantId, contract.Fee, paymentIntentId, booking.Id);
        if (bind.IsFailed)
            throw new BadRequestException(bind.Errors);
    }
}
