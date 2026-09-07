using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Kernel.Enums;
using Concertable.Kernel.ValueObjects;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal sealed class VenueHireConfirmStep : IConfirmStep
{
    private readonly IBus bus;
    private readonly ILogger<VenueHireConfirmStep> logger;

    public VenueHireConfirmStep(
        IBus bus,
        ILogger<VenueHireConfirmStep> logger)
    {
        this.bus = bus;
        this.logger = logger;
    }

    public async Task ConfirmAsync(
        BookingEntity booking,
        CancellationToken ct = default)
    {
        var venueHire = (VenueHireContract)booking.Contract;
        logger.AcceptingVenueHireApplication(
            booking.ApplicationId,
            booking.Id,
            venueHire.HireFee,
            venueHire.ArtistTenantId,
            venueHire.VenueTenantId);
        await bus.SendAsync(new DepositEscrowCommand(
            booking.OperationId,
            PaymentOperationReferences.Escrow(booking.Id),
            venueHire.ArtistTenantId,
            venueHire.VenueTenantId,
            Money.Gbp(venueHire.HireFee).ToMinorUnits(),
            Currency.Gbp,
            venueHire.Commitment,
            PaymentSession.OffSession), ct);
    }
}
