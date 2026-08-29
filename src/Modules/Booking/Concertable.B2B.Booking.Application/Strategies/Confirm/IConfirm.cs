using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;

namespace Concertable.B2B.Booking.Application.Strategies;

internal interface IConfirm : IDealStrategy
{
    Task ConfirmAsync(
        AcceptedApplication application,
        BookingEntity booking,
        CancellationToken ct = default);
}
