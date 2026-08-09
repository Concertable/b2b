using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Enums;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class DepositEscrowAcceptStep : ISimpleAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IEscrowOperationsClient escrowClient;
    private readonly IDealAccessor dealAccessor;
    private readonly ILogger<DepositEscrowAcceptStep> logger;

    public DepositEscrowAcceptStep(
        IBookingService bookingService,
        IEscrowOperationsClient escrowClient,
        IDealAccessor dealAccessor,
        ILogger<DepositEscrowAcceptStep> logger)
    {
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.dealAccessor = dealAccessor;
        this.logger = logger;
    }

    public async Task<UnitResult<AcceptApplicationError>> ExecuteAsync(
        ApplicationEntity application,
        CancellationToken ct = default)
    {
        if (application is not PrepaidApplication prepaid)
            throw new InvalidOperationException("VenueHire acceptance requires a prepaid application.");

        var deal = (VenueHireDeal)dealAccessor.Deal;
        var booking = await bookingService.CreateStandardAsync(application);
        logger.AcceptingVenueHireApplication(application.Id, booking.Id, deal.HireFee, prepaid.ArtistTenantId, prepaid.VenueTenantId);

        return (await escrowClient.DepositAsync(
            prepaid.ArtistTenantId,
            prepaid.VenueTenantId,
            Money.Gbp(deal.HireFee),
            prepaid.PaymentMethodId,
            PaymentSession.OffSession,
            booking.Id,
            ct))
            .MapError(error => (AcceptApplicationError)new AcceptApplicationError.EscrowDepositFailure(error))
            .Bind(_ => UnitResult.Success<AcceptApplicationError>());
    }
}
