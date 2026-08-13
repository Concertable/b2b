using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Enums;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class DepositEscrowAcceptStep(
    IBookingService bookingService,
    IBus bus,
    IDealAccessor dealAccessor,
    ILogger<DepositEscrowAcceptStep> logger) : ISimpleAcceptStep
{
    public async Task<UnitResult<AcceptApplicationError>> ExecuteAsync(
        ApplicationEntity application,
        CancellationToken ct = default)
    {
        if (application is not PrepaidApplication prepaid)
            throw new InvalidOperationException("VenueHire acceptance requires a prepaid application.");

        var booking = await bookingService.CreateStandardAsync(application);
        await StageAsync(prepaid, booking.Id, ct);
        return new Success();
    }

    private async Task StageAsync(PrepaidApplication application, int bookingId, CancellationToken ct)
    {
        var deal = (VenueHireDeal)dealAccessor.Deal;
        logger.AcceptingVenueHireApplication(
            application.Id,
            bookingId,
            deal.HireFee,
            application.ArtistTenantId,
            application.VenueTenantId);

        await bus.SendAsync(new DepositEscrowCommand(
            application.BeginAcceptance(),
            bookingId,
            application.ArtistTenantId,
            application.VenueTenantId,
            Money.Gbp(deal.HireFee).ToMinorUnits(),
            Currency.Gbp,
            application.PaymentMethodId,
            PaymentSession.OffSession), ct);
    }
}
