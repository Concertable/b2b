using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.State;
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
    private readonly IManagerPaymentOperationsClient payment;
    private readonly IBus bus;
    private readonly ILogger<FlatFeeConfirmStep> logger;

    public FlatFeeConfirmStep(
        IBookingService bookings,
        IManagerPaymentOperationsClient payment,
        IBus bus,
        ILogger<FlatFeeConfirmStep> logger)
    {
        this.bookings = bookings;
        this.payment = payment;
        this.bus = bus;
        this.logger = logger;
    }

    public async Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default)
    {
        var accepted = (FlatFeeAcceptedApplication)application;
        var booking = await bookings.CreateStandardAsync(accepted, ct);
        var paymentIntentId = await payment.FindHeldIntentAsync(
            accepted.VenueTenantId,
            accepted.ApplicationId);
        logger.AcceptingFlatFeeApplication(
            accepted.ApplicationId,
            booking.Id,
            paymentIntentId,
            accepted.Fee,
            "GBP",
            accepted.VenueTenantId,
            accepted.ArtistTenantId);
        await bus.SendAsync(new CaptureEscrowCommand(
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
        var booking = await bookings.CreateStandardAsync(accepted, ct);
        logger.AcceptingVenueHireApplication(
            accepted.ApplicationId,
            booking.Id,
            accepted.HireFee,
            accepted.ArtistTenantId,
            accepted.VenueTenantId);
        await bus.SendAsync(new DepositEscrowCommand(
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

internal sealed class DoorSplitConfirmStep : IConfirmStep
{
    private readonly IBookingService bookings;

    public DoorSplitConfirmStep(IBookingService bookings)
    {
        this.bookings = bookings;
    }

    public async Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default)
    {
        var accepted = (DoorSplitAcceptedApplication)application;
        var booking = await bookings.CreateDeferredAsync(accepted, accepted.PaymentMethodId, ct);
        await VerifyPaymentAdvancer.AdvanceAsync(bookings, booking.Id, accepted.Verification, ct);
        return booking;
    }
}

internal sealed class VersusConfirmStep : IConfirmStep
{
    private readonly IBookingService bookings;

    public VersusConfirmStep(IBookingService bookings)
    {
        this.bookings = bookings;
    }

    public async Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default)
    {
        var accepted = (VersusAcceptedApplication)application;
        var booking = await bookings.CreateDeferredAsync(accepted, accepted.PaymentMethodId, ct);
        await VerifyPaymentAdvancer.AdvanceAsync(bookings, booking.Id, accepted.Verification, ct);
        return booking;
    }
}

internal static class VerifyPaymentAdvancer
{
    public static Task AdvanceAsync(
        IBookingService bookings,
        int bookingId,
        VerifyPayment? verification,
        CancellationToken ct) => verification switch
    {
        VerifyPaymentSucceeded succeeded => bookings.RecordSucceededAsync(
            bookingId,
            new FinancialOperationSucceeded(
                succeeded.ApplicationId,
                FinancialOperation.VerifyPayment,
                succeeded.ProviderTransactionId),
            ct),
        VerifyPaymentFailed failed => bookings.RecordFailedAsync(
            bookingId,
            new FinancialOperationFailed(
                failed.ApplicationId,
                FinancialOperation.VerifyPayment,
                failed.ProviderTransactionId,
                new FinancialOperationError(failed.Error.Code, failed.Error.Message)),
            ct),
        null => Task.CompletedTask,
        _ => throw new ArgumentOutOfRangeException(nameof(verification), verification, null)
    };
}
