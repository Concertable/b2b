using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Contract.Contracts;
using Concertable.Kernel.Enums;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class VenueHireAcceptStep : ISimpleAcceptStep
{
    private readonly IApplicationValidator applicationValidator;
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;
    private readonly IPayerLookup payerLookup;
    private readonly IContractAccessor contractAccessor;
    private readonly IApplicationRepository applicationRepository;
    private readonly ILogger<VenueHireAcceptStep> logger;

    public VenueHireAcceptStep(
        IApplicationValidator applicationValidator,
        IBookingService bookingService,
        IEscrowClient escrowClient,
        IPayerLookup payerLookup,
        IContractAccessor contractAccessor,
        IApplicationRepository applicationRepository,
        ILogger<VenueHireAcceptStep> logger)
    {
        this.applicationValidator = applicationValidator;
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.payerLookup = payerLookup;
        this.contractAccessor = contractAccessor;
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

        var contract = (VenueHireContract)contractAccessor.Contract;
        var booking = await bookingService.CreateStandardAsync(applicationId, contract.ContractType);

        logger.AcceptingVenueHireApplication(applicationId, booking.Id, contract.HireFee, artistManagerId, venueManagerId);

        var hold = await escrowClient.DepositAsync(artistManagerId, venueManagerId, contract.HireFee, prepaid.PaymentMethodId, PaymentSession.OffSession, booking.Id);
        if (hold.IsFailed)
            throw new BadRequestException(hold.Errors);
    }
}
