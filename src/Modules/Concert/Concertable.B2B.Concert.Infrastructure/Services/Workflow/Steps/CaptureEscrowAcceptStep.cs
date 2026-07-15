using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class CaptureEscrowAcceptStep : ISimpleAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;
    private readonly IApplicationRepository applicationRepository;
    private readonly IDealAccessor dealAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ILogger<CaptureEscrowAcceptStep> logger;

    public CaptureEscrowAcceptStep(
        IBookingService bookingService,
        IEscrowClient escrowClient,
        IApplicationRepository applicationRepository,
        IDealAccessor dealAccessor,
        IManagerPaymentClient managerPaymentClient,
        ILogger<CaptureEscrowAcceptStep> logger)
    {
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.applicationRepository = applicationRepository;
        this.dealAccessor = dealAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        /* FlatFee: the venue tenant pays the artist tenant, per the application's frozen snapshot. */
        var (venueTenantId, artistTenantId) = await applicationRepository.GetTenantPairAsync(applicationId)
            .OrNotFound(DisplayNames.Application);
        var deal = (FlatFeeDeal)dealAccessor.Deal;
        var booking = await bookingService.CreateStandardAsync(applicationId, deal.DealType);

        var paymentIntentId = await managerPaymentClient.FindHeldIntentAsync(venueTenantId, applicationId);

        logger.AcceptingFlatFeeApplication(applicationId, booking.Id, paymentIntentId, deal.Fee, "GBP", venueTenantId, artistTenantId);

        var bind = await escrowClient.CaptureAsync(venueTenantId, artistTenantId, deal.Fee, paymentIntentId, booking.Id);
        if (bind.IsFailed)
            throw new BadRequestException(bind.Errors);
    }
}
