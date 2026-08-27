using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.Kernel.Enums;
using Concertable.Kernel.ValueObjects;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class VenueHireConfirmStep : IConfirmStep
{
    private readonly IBookingService bookings;
    private readonly IBus bus;
    private readonly ILogger<VenueHireConfirmStep> logger;

    public VenueHireConfirmStep(
        IBookingService bookings,
        IBus bus,
        ILogger<VenueHireConfirmStep> logger)
    {
        this.bookings = bookings;
        this.bus = bus;
        this.logger = logger;
    }

    public async Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default)
    {
        var accepted = (VenueHireAcceptedApplication)application;
        var booking = await this.bookings.CreateStandardAsync(accepted, ct);
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
        return booking;
    }
}
