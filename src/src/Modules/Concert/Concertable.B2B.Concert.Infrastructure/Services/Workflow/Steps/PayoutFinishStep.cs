using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.Kernel.Enums;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PayoutFinishStep : IFinishStep
{
    private readonly IBookingService bookingService;
    private readonly IConcertRepository concertRepository;
    private readonly IDealAccessor dealAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly IArtistShareCalculator artistShareCalculator;
    private readonly ILogger<PayoutFinishStep> logger;

    public PayoutFinishStep(
        IBookingService bookingService,
        IConcertRepository concertRepository,
        IDealAccessor dealAccessor,
        IManagerPaymentClient managerPaymentClient,
        IArtistShareCalculator artistShareCalculator,
        ILogger<PayoutFinishStep> logger)
    {
        this.bookingService = bookingService;
        this.concertRepository = concertRepository;
        this.dealAccessor = dealAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.artistShareCalculator = artistShareCalculator;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int concertId)
    {
        var doorRevenue = await concertRepository.GetDoorRevenueByConcertIdAsync(concertId)
            ?? throw new InvalidOperationException(
                $"Concert {concertId} reached settlement with no declared door revenue — the completion gate should make this unreachable.");
        var artistShare = artistShareCalculator.Calculate(dealAccessor.Deal, doorRevenue);

        logger.ArtistShareCalculated(concertId, doorRevenue, artistShare);

        /* DoorSplit/Versus: the venue tenant pays the artist tenant, per the booking's frozen snapshot. */
        var settlement = await bookingService.GetSettlementByConcertIdAsync(concertId);

        logger.SettlingConcert(concertId, settlement.BookingId, artistShare, settlement.VenueTenantId, settlement.ArtistTenantId);

        var payment = await managerPaymentClient.PayAsync(
            settlement.VenueTenantId,
            settlement.ArtistTenantId,
            artistShare,
            settlement.PaymentMethodId,
            PaymentSession.OffSession,
            settlement.BookingId);
        if (payment.IsFailed)
            throw new BadRequestException(payment.Errors);
    }
}
