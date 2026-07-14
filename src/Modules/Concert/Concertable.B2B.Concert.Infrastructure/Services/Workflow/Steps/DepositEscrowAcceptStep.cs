using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Enums;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class DepositEscrowAcceptStep : ISimpleAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;
    private readonly IDealAccessor dealAccessor;
    private readonly IApplicationRepository applicationRepository;
    private readonly ILogger<DepositEscrowAcceptStep> logger;

    public DepositEscrowAcceptStep(
        IBookingService bookingService,
        IEscrowClient escrowClient,
        IDealAccessor dealAccessor,
        IApplicationRepository applicationRepository,
        ILogger<DepositEscrowAcceptStep> logger)
    {
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.dealAccessor = dealAccessor;
        this.applicationRepository = applicationRepository;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        var application = await applicationRepository.GetByIdAsync(applicationId)
            .OrNotFound();
        if (application is not PrepaidApplication prepaid)
            throw new BadRequestException("VenueHire requires a PrepaidApplication");

        var deal = (VenueHireDeal)dealAccessor.Deal;
        var booking = await bookingService.CreateStandardAsync(applicationId, deal.DealType);

        /* VenueHire: the artist hires the venue, so the artist tenant pays the venue tenant —
           both read off the application's frozen snapshot. */
        logger.AcceptingVenueHireApplication(applicationId, booking.Id, deal.HireFee, prepaid.ArtistTenantId, prepaid.VenueTenantId);

        var hold = await escrowClient.DepositAsync(prepaid.ArtistTenantId, prepaid.VenueTenantId, deal.HireFee, prepaid.PaymentMethodId, PaymentSession.OffSession, booking.Id);
        if (hold.IsFailed)
            throw new BadRequestException(hold.Errors);
    }
}
