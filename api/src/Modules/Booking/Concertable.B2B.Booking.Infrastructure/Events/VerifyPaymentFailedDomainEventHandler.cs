using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class VerifyPaymentFailedDomainEventHandler : IPreCommitDomainEventHandler<VerifyPaymentFailedDomainEvent>
{
    private readonly IBookingPrivilegedRepository bookingRepository;
    private readonly IBookingWorkflow bookingWorkflow;
    private readonly IPrivilegedUnitOfWorkBehavior unitOfWork;

    public VerifyPaymentFailedDomainEventHandler(
        IBookingPrivilegedRepository bookingRepository,
        IBookingWorkflow bookingWorkflow,
        IPrivilegedUnitOfWorkBehavior unitOfWork)
    {
        this.bookingRepository = bookingRepository;
        this.bookingWorkflow = bookingWorkflow;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(VerifyPaymentFailedDomainEvent @event, CancellationToken ct = default)
        => await unitOfWork.ExecuteAsync(async () =>
    {
        var payment = @event.Payment;
        var bookingId = await bookingRepository.GetIdByApplicationIdAsync(payment.ApplicationId, ct);
        if (bookingId is null)
            return;

        await bookingWorkflow.RecordFailedAsync(
            bookingId.Value,
            new VerifyPaymentFailedEvidence(
                payment.ApplicationId,
                new FinancialOperationError(payment.Error.Code, payment.Error.Message)),
            ct);
    }, ct);
}
