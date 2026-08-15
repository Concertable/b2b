using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.Kernel.Enums;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PayoutFinishStep : IFinishStep
{
    private readonly IBookingService bookingService;
    private readonly ISettlementAmountResolver settlementAmountResolver;
    private readonly IDealAccessor dealAccessor;
    private readonly IManagerPaymentOperationsClient managerPaymentClient;
    private readonly ILogger<PayoutFinishStep> logger;

    public PayoutFinishStep(
        IBookingService bookingService,
        ISettlementAmountResolver settlementAmountResolver,
        IDealAccessor dealAccessor,
        IManagerPaymentOperationsClient managerPaymentClient,
        ILogger<PayoutFinishStep> logger)
    {
        this.bookingService = bookingService;
        this.settlementAmountResolver = settlementAmountResolver;
        this.dealAccessor = dealAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task<UnitResult<FinishConcertError>> ExecuteAsync(int concertId, CancellationToken ct = default)
    {
        var artistShare = await settlementAmountResolver.ResolveGrossAsync(concertId, dealAccessor.Deal);

        logger.ArtistShareCalculated(concertId, artistShare.Amount);
        var settlement = await bookingService.GetSettlementByConcertIdAsync(concertId);

        logger.SettlingConcert(concertId, settlement.BookingId, artistShare.Amount, settlement.VenueTenantId, settlement.ArtistTenantId);

        return (await managerPaymentClient.PayAsync(
            settlement.VenueTenantId,
            settlement.ArtistTenantId,
            artistShare,
            settlement.PaymentMethodId,
            PaymentSession.OffSession,
            settlement.BookingId,
            ct)).Bind(
                _ => UnitResult.Success<FinishConcertError>(),
                error => new FinishConcertError.ManagerPaymentFailure(error));
    }
}
