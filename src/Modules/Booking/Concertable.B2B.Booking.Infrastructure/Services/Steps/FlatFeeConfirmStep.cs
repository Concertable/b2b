using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.Kernel.Enums;
using Concertable.Kernel.ValueObjects;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class FlatFeeConfirmStep : IConfirmStep
{
    private readonly IBookingService bookings;
    private readonly IManagerPaymentOperationsClient managerPaymentClient;
    private readonly IBus bus;
    private readonly ILogger<FlatFeeConfirmStep> logger;

    public FlatFeeConfirmStep(
        IBookingService bookings,
        IManagerPaymentOperationsClient managerPaymentClient,
        IBus bus,
        ILogger<FlatFeeConfirmStep> logger)
    {
        this.bookings = bookings;
        this.managerPaymentClient = managerPaymentClient;
        this.bus = bus;
        this.logger = logger;
    }

    public async Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default)
    {
        var accepted = (FlatFeeAcceptedApplication)application;
        var booking = await this.bookings.CreateStandardAsync(accepted, ct);
        var paymentIntentId = await this.managerPaymentClient.FindHeldIntentAsync(
            accepted.VenueTenantId,
            accepted.ApplicationId);
        this.logger.AcceptingFlatFeeApplication(
            accepted.ApplicationId,
            booking.Id,
            paymentIntentId,
            accepted.Fee,
            "GBP",
            accepted.VenueTenantId,
            accepted.ArtistTenantId);
        await this.bus.SendAsync(new CaptureEscrowCommand(
            accepted.OperationId,
            booking.Id,
            accepted.VenueTenantId,
            accepted.ArtistTenantId,
            Money.Gbp(accepted.Fee).ToMinorUnits(),
            Currency.Gbp,
            paymentIntentId), ct);
        return booking;
    }
}
