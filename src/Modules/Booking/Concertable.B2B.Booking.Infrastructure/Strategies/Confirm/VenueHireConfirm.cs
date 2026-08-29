using Concertable.B2B.Application.Contracts;
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
        AcceptedApplication application,
        BookingEntity booking,
        CancellationToken ct = default)
    {
        var accepted = (VenueHireAcceptedApplication)application;
        this.logger.AcceptingVenueHireApplication(
            accepted.ApplicationId,
            booking.Id,
            accepted.HireFee,
            accepted.ArtistTenantId,
            accepted.VenueTenantId);
        await this.bus.SendAsync(new DepositEscrowCommand(
            accepted.OperationId,
            booking.Id,
            accepted.ArtistTenantId,
            accepted.VenueTenantId,
            Money.Gbp(accepted.HireFee).ToMinorUnits(),
            Currency.Gbp,
            accepted.PaymentMethodId,
            PaymentSession.OffSession), ct);
    }
}
