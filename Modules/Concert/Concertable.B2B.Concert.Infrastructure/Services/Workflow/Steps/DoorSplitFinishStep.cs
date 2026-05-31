using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Enums;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal class DoorSplitFinishStep : IFinishStep
{
    private readonly IBookingService bookingService;
    private readonly IConcertRepository concertRepository;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ILogger<DoorSplitFinishStep> logger;

    public DoorSplitFinishStep(
        IBookingService bookingService,
        IConcertRepository concertRepository,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        ILogger<DoorSplitFinishStep> logger)
    {
        this.bookingService = bookingService;
        this.concertRepository = concertRepository;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int concertId)
    {
        var contract = (DoorSplitContract)contractAccessor.Contract;
        var totalRevenue = await concertRepository.GetTotalRevenueByConcertIdAsync(concertId);
        var artistShare = totalRevenue * (contract.ArtistDoorPercent / 100);

        logger.DoorSplitArtistShareCalculated(concertId, totalRevenue, contract.ArtistDoorPercent, artistShare);

        var settlement = await bookingService.MarkAwaitingPaymentByConcertIdAsync(concertId);

        logger.SettlingConcert(concertId, settlement.BookingId, artistShare, settlement.VenueUserId, settlement.ArtistUserId);

        var payment = await managerPaymentClient.PayAsync(
            settlement.VenueUserId,
            settlement.ArtistUserId,
            artistShare,
            settlement.PaymentMethodId,
            PaymentSession.OffSession,
            settlement.BookingId);
        if (payment.IsFailed)
            throw new BadRequestException(payment.Errors);
    }
}
