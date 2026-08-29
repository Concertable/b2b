using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class CancellationExecutor : ICancellationExecutor
{
    private readonly IDealStrategyFactory<ICancelStep> cancelStepFactory;

    public CancellationExecutor(IDealStrategyFactory<ICancelStep> cancelStepFactory)
    {
        this.cancelStepFactory = cancelStepFactory;
    }

    public Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default) =>
        this.cancelStepFactory.Create(booking.DealType).ExecuteAsync(booking, ct);
}
