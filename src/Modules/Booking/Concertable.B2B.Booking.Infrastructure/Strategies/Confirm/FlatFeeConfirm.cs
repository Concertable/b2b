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
    private readonly IManagerPaymentOperationsClient managerPaymentOperationsClient;
    private readonly IBus bus;
    private readonly ILogger<FlatFeeConfirm> logger;

    public FlatFeeConfirm(
        IManagerPaymentOperationsClient managerPaymentOperationsClient,
        IBus bus,
        ILogger<FlatFeeConfirm> logger)
    {
        this.managerPaymentOperationsClient = managerPaymentOperationsClient;
        this.bus = bus;
        this.logger = logger;
    }

    public async Task ConfirmAsync(
        BookingEntity booking,
        CancellationToken ct = default)
    {
        var flatFee = (FlatFeeContract)booking.Contract;
        var paymentIntentId = await managerPaymentOperationsClient.FindHeldIntentAsync(
            flatFee.VenueTenantId,
            booking.ApplicationId);
        logger.AcceptingFlatFeeApplication(
            booking.ApplicationId,
            booking.Id,
            paymentIntentId,
            flatFee.Fee,
            "GBP",
            flatFee.VenueTenantId,
            flatFee.ArtistTenantId);
        await bus.SendAsync(new CaptureEscrowCommand(
            booking.OperationId,
            booking.Id,
            flatFee.VenueTenantId,
            flatFee.ArtistTenantId,
            Money.Gbp(flatFee.Fee).ToMinorUnits(),
            Currency.Gbp,
            paymentIntentId), ct);
    }
}
