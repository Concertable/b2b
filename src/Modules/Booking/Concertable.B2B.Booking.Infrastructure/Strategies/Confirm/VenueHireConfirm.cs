using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.Kernel.Enums;
using Concertable.Kernel.ValueObjects;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal sealed class VenueHireConfirm : IConfirm
{
    private readonly IBus bus;
    private readonly ILogger<VenueHireConfirm> logger;

    public VenueHireConfirm(
        IBus bus,
        ILogger<VenueHireConfirm> logger)
    {
        this.bus = bus;
        this.logger = logger;
    }

    public async Task ConfirmAsync(
        ContractEntity contract,
        BookingEntity booking,
        CancellationToken ct = default)
    {
        var venueHire = (VenueHireContract)contract;
        logger.AcceptingVenueHireApplication(
            booking.ApplicationId,
            booking.Id,
            venueHire.HireFee,
            venueHire.ArtistTenantId,
            venueHire.VenueTenantId);
        await bus.SendAsync(new DepositEscrowCommand(
            booking.OperationId,
            booking.Id,
            venueHire.ArtistTenantId,
            venueHire.VenueTenantId,
            Money.Gbp(venueHire.HireFee).ToMinorUnits(),
            Currency.Gbp,
            venueHire.PaymentMethodId,
            PaymentSession.OffSession), ct);
    }
}
