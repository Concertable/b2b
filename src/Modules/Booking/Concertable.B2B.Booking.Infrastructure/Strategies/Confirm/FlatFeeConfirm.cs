using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.Kernel.Enums;
using Concertable.Kernel.ValueObjects;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal sealed class FlatFeeConfirm : IConfirm
{
    private readonly IManagerPaymentOperationsClient managerPaymentClient;
    private readonly IBus bus;
    private readonly ILogger<FlatFeeConfirm> logger;

    public FlatFeeConfirm(
        IManagerPaymentOperationsClient managerPaymentClient,
        IBus bus,
        ILogger<FlatFeeConfirm> logger)
    {
        this.managerPaymentClient = managerPaymentClient;
        this.bus = bus;
        this.logger = logger;
    }

    public async Task ConfirmAsync(
        AcceptedApplication application,
        BookingEntity booking,
        CancellationToken ct = default)
    {
        var accepted = (FlatFeeAcceptedApplication)application;
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
    }
}
