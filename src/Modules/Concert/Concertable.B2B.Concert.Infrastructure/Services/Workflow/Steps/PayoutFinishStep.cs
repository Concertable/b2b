using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Enums;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PayoutFinishStep : IFinishStep
{
    private readonly IBookingService bookingService;
    private readonly IConcertRepository concertRepository;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly IArtistShareCalculator artistShareCalculator;
    private readonly ITenantModule tenantModule;
    private readonly ILogger<PayoutFinishStep> logger;

    public PayoutFinishStep(
        IBookingService bookingService,
        IConcertRepository concertRepository,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        IArtistShareCalculator artistShareCalculator,
        ITenantModule tenantModule,
        ILogger<PayoutFinishStep> logger)
    {
        this.bookingService = bookingService;
        this.concertRepository = concertRepository;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.artistShareCalculator = artistShareCalculator;
        this.tenantModule = tenantModule;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int concertId)
    {
        var totalRevenue = await concertRepository.GetTotalRevenueByConcertIdAsync(concertId);
        var artistShare = artistShareCalculator.Calculate(contractAccessor.Contract, totalRevenue);

        logger.ArtistShareCalculated(concertId, totalRevenue, artistShare);

        var settlement = await bookingService.GetSettlementByConcertIdAsync(concertId);

        logger.SettlingConcert(concertId, settlement.BookingId, artistShare, settlement.VenueUserId, settlement.ArtistUserId);

        var payerOwnerId = await tenantModule.GetTenantIdByUserIdAsync(settlement.VenueUserId)
            ?? throw new NotFoundException($"No tenant for user {settlement.VenueUserId}");
        var payeeOwnerId = await tenantModule.GetTenantIdByUserIdAsync(settlement.ArtistUserId)
            ?? throw new NotFoundException($"No tenant for user {settlement.ArtistUserId}");

        var payment = await managerPaymentClient.PayAsync(
            payerOwnerId,
            payeeOwnerId,
            artistShare,
            settlement.PaymentMethodId,
            PaymentSession.OffSession,
            settlement.BookingId);
        if (payment.IsFailed)
            throw new BadRequestException(payment.Errors);
    }
}
