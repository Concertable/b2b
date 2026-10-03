using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class VerifyPaymentSucceededDomainEventHandler : IPreCommitDomainEventHandler<VerifyPaymentSucceededDomainEvent>
{
    private readonly IBookingPrivilegedRepository bookingRepository;
    private readonly IBookingWorkflow bookingWorkflow;
    private readonly IPrivilegedUnitOfWorkBehavior unitOfWork;

    public VerifyPaymentSucceededDomainEventHandler(
        IBookingPrivilegedRepository bookingRepository,
        IBookingWorkflow bookingWorkflow,
        IPrivilegedUnitOfWorkBehavior unitOfWork)
    {
        this.bookingRepository = bookingRepository;
        this.bookingWorkflow = bookingWorkflow;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(VerifyPaymentSucceededDomainEvent @event, CancellationToken ct = default)
        => await unitOfWork.ExecuteAsync(async () =>
    {
        var payment = @event.Payment;
        var bookingId = await bookingRepository.GetIdByApplicationIdAsync(payment.ApplicationId, ct);
        if (bookingId is null)
            return;

        await bookingWorkflow.RecordSucceededAsync(
            bookingId.Value,
            new VerifyPaymentSucceededEvidence(payment.ApplicationId),
            ct);
    }, ct);
}
