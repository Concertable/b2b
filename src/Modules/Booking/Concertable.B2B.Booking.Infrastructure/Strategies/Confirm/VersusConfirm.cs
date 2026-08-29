using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal sealed class VersusConfirm : IConfirm
{
    public Task ConfirmAsync(
        AcceptedApplication application,
        BookingEntity booking,
        CancellationToken ct = default)
    {
        var accepted = (VersusAcceptedApplication)application;
        if (accepted.Verification is { } verification)
            VerifyPaymentAdvancer.Advance(booking, verification);
        return Task.CompletedTask;
    }
}
