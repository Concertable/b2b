using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class CaptureEscrowAcceptStep(
    IBookingService bookingService,
    IBus bus,
    IDealAccessor dealAccessor,
    IManagerPaymentOperationsClient managerPaymentClient,
    ILogger<CaptureEscrowAcceptStep> logger) : ISimpleAcceptStep
{
    public async Task<UnitResult<AcceptApplicationError>> ExecuteAsync(
        ApplicationEntity application,
        CancellationToken ct = default)
    {
        var booking = await bookingService.CreateStandardAsync(application);
        await StageAsync(application, booking.Id, ct);
        return new Success();
    }

    private async Task StageAsync(ApplicationEntity application, int bookingId, CancellationToken ct)
    {
        var deal = (FlatFeeDealDto)dealAccessor.Deal;
        var paymentIntentId = await managerPaymentClient.FindHeldIntentAsync(application.VenueTenantId, application.Id);

        logger.AcceptingFlatFeeApplication(
            application.Id,
            bookingId,
            paymentIntentId,
            deal.Fee,
            "GBP",
            application.VenueTenantId,
            application.ArtistTenantId);

        await bus.SendAsync(new CaptureEscrowCommand(
            application.BeginAcceptance(),
            bookingId,
            application.VenueTenantId,
            application.ArtistTenantId,
            Money.Gbp(deal.Fee).ToMinorUnits(),
            Currency.Gbp,
            paymentIntentId), ct);
    }
}
