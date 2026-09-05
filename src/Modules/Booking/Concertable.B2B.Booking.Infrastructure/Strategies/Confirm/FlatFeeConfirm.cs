using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Kernel.Enums;
using Concertable.Kernel.ValueObjects;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal sealed class FlatFeeConfirm : IConfirm
{
    private readonly IBus bus;
    private readonly ILogger<FlatFeeConfirm> logger;

    public FlatFeeConfirm(
        IBus bus,
        ILogger<FlatFeeConfirm> logger)
    {
        this.bus = bus;
        this.logger = logger;
    }

    public async Task ConfirmAsync(
        BookingEntity booking,
        CancellationToken ct = default)
    {
        var flatFee = (FlatFeeContract)booking.Contract;
        logger.AcceptingFlatFeeApplication(
            booking.ApplicationId,
            booking.Id,
            flatFee.Commitment.ClientReference,
            flatFee.Fee,
            "GBP",
            flatFee.VenueTenantId,
            flatFee.ArtistTenantId);
        await bus.SendAsync(new CaptureEscrowCommand(
            booking.OperationId,
            PaymentOperationReferences.Escrow(booking.Id),
            flatFee.VenueTenantId,
            flatFee.ArtistTenantId,
            Money.Gbp(flatFee.Fee).ToMinorUnits(),
            Currency.Gbp,
            flatFee.Commitment), ct);
    }
}
