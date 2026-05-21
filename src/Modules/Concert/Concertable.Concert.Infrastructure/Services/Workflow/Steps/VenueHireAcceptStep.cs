using Concertable.Concert.Application.Workflow.Steps;
using Concertable.Contract.Contracts;
using Concertable.Shared.Enums;
using Concertable.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class VenueHireAcceptStep : ISimpleAcceptStep
{
    private readonly IApplicationValidator applicationValidator;
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;
    private readonly IPayerLookup payerLookup;
    private readonly IContractLoader contractLoader;
    private readonly IApplicationRepository applicationRepository;
    private readonly ILogger<VenueHireAcceptStep> logger;

    public VenueHireAcceptStep(
        IApplicationValidator applicationValidator,
        IBookingService bookingService,
        IEscrowClient escrowClient,
        IPayerLookup payerLookup,
        IContractLoader contractLoader,
        IApplicationRepository applicationRepository,
        ILogger<VenueHireAcceptStep> logger)
    {
        this.applicationValidator = applicationValidator;
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.payerLookup = payerLookup;
        this.contractLoader = contractLoader;
        this.applicationRepository = applicationRepository;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);

        var (venueManagerId, artistManagerId) = await payerLookup.GetManagerIdsAsync(applicationId)
            ?? throw new NotFoundException("Application not found");

        var application = await applicationRepository.GetByIdAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        if (application is not PrepaidApplication prepaid)
            throw new BadRequestException("VenueHire requires a PrepaidApplication");

        var contract = (VenueHireContract)await contractLoader.LoadByApplicationIdAsync(applicationId);
        var booking = await bookingService.CreateStandardAsync(applicationId);

        logger.LogInformation(
            "Accepting application {ApplicationId} (booking {BookingId}): charging {Amount} GBP from {PayerId} on behalf of {PayeeId}",
            applicationId, booking.Id, contract.HireFee, artistManagerId, venueManagerId);

        var hold = await escrowClient.DepositAsync(artistManagerId, venueManagerId, contract.HireFee, prepaid.PaymentMethodId, PaymentSession.OffSession, booking.Id);
        if (hold.IsFailed)
            throw new BadRequestException(hold.Errors);
    }
}
