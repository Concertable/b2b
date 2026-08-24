using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.State;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;

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

internal sealed class ImmediateCancelStep : ICancelStep
{
    public Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default)
    {
        booking.BeginCancellation();
        booking.Cancel();
        return Task.CompletedTask;
    }
}

internal sealed class EscrowCancelStep : ICancelStep
{
    private readonly IBus bus;

    public EscrowCancelStep(IBus bus)
    {
        this.bus = bus;
    }

    public async Task ExecuteAsync(BookingEntity booking, CancellationToken ct = default)
    {
        if (booking.State == BookingState.ConfirmationFailed)
        {
            booking.BeginCancellation();
            booking.Cancel();
            return;
        }

        await this.bus.SendAsync(new RefundEscrowCommand(
            booking.BeginCancellation(),
            booking.Id,
            RefundReasonCodes.RequestedByCustomer), ct);
    }
}
