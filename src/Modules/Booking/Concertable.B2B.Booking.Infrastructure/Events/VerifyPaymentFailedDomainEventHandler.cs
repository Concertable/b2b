using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class VerifyPaymentFailedDomainEventHandler : IPreCommitDomainEventHandler<VerifyPaymentFailedDomainEvent>
{
    private readonly IBookingService bookingService;

    public VerifyPaymentFailedDomainEventHandler(IBookingService bookingService)
    {
        this.bookingService = bookingService;
    }

    public async Task HandleAsync(VerifyPaymentFailedDomainEvent @event, CancellationToken ct = default)
    {
        var payment = @event.Payment;
        var bookingId = await bookingService.GetIdByApplicationIdAsync(payment.ApplicationId, ct);
        if (bookingId is null)
            return;

        await bookingService.RecordFailedAsync(
            bookingId.Value,
            new VerifyPaymentFailedEvidence(
                payment.ApplicationId,
                new FinancialOperationError(payment.Error.Code, payment.Error.Message)),
            ct);
    }
}
