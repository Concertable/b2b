using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class CancellationExecutor : ICancellationExecutor
{
    private readonly IDealStrategyFactory<ICancelStep> steps;

    public CancellationExecutor(IDealStrategyFactory<ICancelStep> steps)
    {
        this.steps = steps;
    }

    public Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default) =>
        this.steps.Create(booking.DealType).ExecuteAsync(booking, ct);
}
