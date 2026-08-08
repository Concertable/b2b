using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class CaptureEscrowAcceptStep : ISimpleAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IEscrowOperationsClient escrowClient;
    private readonly IApplicationRepository applicationRepository;
    private readonly IDealAccessor dealAccessor;
    private readonly IManagerPaymentOperationsClient managerPaymentClient;
    private readonly ILogger<CaptureEscrowAcceptStep> logger;

    public CaptureEscrowAcceptStep(
        IBookingService bookingService,
        IEscrowOperationsClient escrowClient,
        IApplicationRepository applicationRepository,
        IDealAccessor dealAccessor,
        IManagerPaymentOperationsClient managerPaymentClient,
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

        var bind = await escrowClient.CaptureAsync(venueTenantId, artistTenantId, Money.Gbp(deal.Fee), paymentIntentId, booking.Id);
        if (bind.TryGetError(out var error))
            throw new BadRequestException(error.Definition.Message);
    }
}
