using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Enums;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal class DoorSplitFinishStep : IFinishStep
{
    private readonly IBookingService bookingService;
    private readonly IBookingRepository bookingRepository;
    private readonly IConcertRepository concertRepository;
    private readonly IContractLoader contractLoader;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly ILogger<DoorSplitFinishStep> logger;

    public DoorSplitFinishStep(
        IBookingService bookingService,
        IBookingRepository bookingRepository,
        IConcertRepository concertRepository,
        IContractLoader contractLoader,
        IManagerPaymentClient managerPaymentClient,
        ILogger<DoorSplitFinishStep> logger)
    {
        this.bookingService = bookingService;
        this.bookingRepository = bookingRepository;
        this.concertRepository = concertRepository;
        this.contractLoader = contractLoader;
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int concertId)
    {
        var booking = await bookingRepository.GetByConcertIdAsync(concertId)
            ?? throw new NotFoundException("Booking not found");

        var contract = (DoorSplitContract)await contractLoader.LoadByConcertIdAsync(concertId);
        var totalRevenue = await concertRepository.GetTotalRevenueByConcertIdAsync(concertId);
        var artistShare = totalRevenue * (contract.ArtistDoorPercent / 100);

        logger.DoorSplitArtistShareCalculated(concertId, totalRevenue, contract.ArtistDoorPercent, artistShare);

        var marked = await bookingService.MarkAwaitingPaymentByConcertIdAsync(concertId);
        if (marked is not DeferredBooking deferred)
            throw new BadRequestException("Concert finish requires a DeferredBooking");

        logger.SettlingConcert(concertId, marked.Id, artistShare, booking.Application.Opportunity.Venue.UserId, booking.Application.Artist.UserId);

        var payment = await managerPaymentClient.PayAsync(
            booking.Application.Opportunity.Venue.UserId,
            booking.Application.Artist.UserId,
            artistShare,
            deferred.PaymentMethodId,
            PaymentSession.OffSession,
            marked.Id);
        if (payment.IsFailed)
            throw new BadRequestException(payment.Errors);
    }
}
