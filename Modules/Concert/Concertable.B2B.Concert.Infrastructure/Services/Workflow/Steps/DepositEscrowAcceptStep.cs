using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Enums;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class DepositEscrowAcceptStep : ISimpleAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IEscrowClient escrowClient;
    private readonly IPayerLookup payerLookup;
    private readonly IContractAccessor contractAccessor;
    private readonly IApplicationRepository applicationRepository;
    private readonly ITenantModule tenantModule;
    private readonly ILogger<DepositEscrowAcceptStep> logger;

    public DepositEscrowAcceptStep(
        IBookingService bookingService,
        IEscrowClient escrowClient,
        IPayerLookup payerLookup,
        IContractAccessor contractAccessor,
        IApplicationRepository applicationRepository,
        ITenantModule tenantModule,
        ILogger<DepositEscrowAcceptStep> logger)
    {
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.payerLookup = payerLookup;
        this.contractAccessor = contractAccessor;
        this.applicationRepository = applicationRepository;
        this.tenantModule = tenantModule;
        this.logger = logger;
    }

    public async Task ExecuteAsync(int applicationId)
    {
        var (venueManagerId, artistManagerId) = await payerLookup.GetManagerIdsAsync(applicationId)
            ?? throw new NotFoundException("Application not found");

        var application = await applicationRepository.GetByIdAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        if (application is not PrepaidApplication prepaid)
            throw new BadRequestException("VenueHire requires a PrepaidApplication");

        var contract = (VenueHireContract)contractAccessor.Contract;
        var booking = await bookingService.CreateStandardAsync(applicationId, contract.ContractType);

        logger.AcceptingVenueHireApplication(applicationId, booking.Id, contract.HireFee, artistManagerId, venueManagerId);

        var payerOwnerId = await tenantModule.GetTenantIdByUserIdAsync(artistManagerId)
            ?? throw new NotFoundException($"No tenant for user {artistManagerId}");
        var payeeOwnerId = await tenantModule.GetTenantIdByUserIdAsync(venueManagerId)
            ?? throw new NotFoundException($"No tenant for user {venueManagerId}");

        var hold = await escrowClient.DepositAsync(payerOwnerId, payeeOwnerId, contract.HireFee, prepaid.PaymentMethodId, PaymentSession.OffSession, booking.Id);
        if (hold.IsFailed)
            throw new BadRequestException(hold.Errors);
    }
}
