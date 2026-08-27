using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingCancellationExecutor : IBookingCancellationExecutor
{
    private readonly IBookingDealStrategyFactory<ICancelStep> steps;

    public BookingCancellationExecutor(IBookingDealStrategyFactory<ICancelStep> steps)
    {
        this.steps = steps;
    }

    public Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default) =>
        this.steps.Create(booking.DealType).ExecuteAsync(booking, ct);
}
