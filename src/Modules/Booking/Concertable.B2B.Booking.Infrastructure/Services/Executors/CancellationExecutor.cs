using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class CancellationExecutor : ICancellationExecutor
{
    private readonly IDealTypeStrategyFactory<ICancelStep> steps;

    public CancellationExecutor(IDealTypeStrategyFactory<ICancelStep> steps)
    {
        this.steps = steps;
    }

    public Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default) =>
        this.steps.Create(booking.DealType).ExecuteAsync(booking, ct);
}
