using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal sealed class DeferredConfirm : IConfirm
{
    public Task ConfirmAsync(
        ContractEntity contract,
        BookingEntity booking,
        CancellationToken ct = default) => Task.CompletedTask;
}
