using Concertable.B2B.Concert.Application.Interfaces;
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
    private readonly ISettlementAmountResolver settlementAmountResolver;
    private readonly IDealAccessor dealAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ILogger<PayoutFinishStep> logger;

    public PayoutFinishStep(
        IBookingService bookingService,
        ISettlementAmountResolver settlementAmountResolver,
        IDealAccessor dealAccessor,
        IManagerPaymentClient managerPaymentClient,
        ILogger<PayoutFinishStep> logger)
    {
        this.bookingService = bookingService;
        this.settlementAmountResolver = settlementAmountResolver;
        this.dealAccessor = dealAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int concertId)
    {
        // Same resolver the invoice issuer uses, so the charged share and the invoiced gross can't diverge.
        var artistShare = await settlementAmountResolver.ResolveGrossAsync(concertId, dealAccessor.Deal);

        logger.ArtistShareCalculated(concertId, artistShare);

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
