using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal sealed class BookingConfirmationExecutor : IBookingConfirmationExecutor
{
    private readonly IBookingDealStrategyFactory<IConfirmStep> steps;
    private readonly IOutboxUnitOfWorkBehavior outbox;

    public BookingConfirmationExecutor(
        IBookingDealStrategyFactory<IConfirmStep> steps,
        IOutboxUnitOfWorkBehavior outbox)
    {
        this.steps = steps;
        this.outbox = outbox;
    }

    public Task<BookingDto> ExecuteAsync(
        AcceptedApplication application,
        CancellationToken ct = default) =>
        this.outbox.ExecuteAsync(
            () => this.steps.Create(application.DealType).ExecuteAsync(application, ct),
            ct);
}
