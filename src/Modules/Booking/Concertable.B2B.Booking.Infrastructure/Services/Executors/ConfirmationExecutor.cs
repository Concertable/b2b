using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class ConfirmationExecutor : IConfirmationExecutor
{
    private readonly IDealTypeStrategyFactory<IConfirmStep> confirmStepFactory;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;

    public ConfirmationExecutor(
        IDealTypeStrategyFactory<IConfirmStep> confirmStepFactory,
        IOutboxUnitOfWorkBehavior outboxBehavior)
    {
        this.confirmStepFactory = confirmStepFactory;
        this.outboxBehavior = outboxBehavior;
    }

    public Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default) =>
        this.outboxBehavior.ExecuteAsync(
            () => this.confirmStepFactory.Create(application.DealType).ExecuteAsync(application, ct),
            ct);
}
