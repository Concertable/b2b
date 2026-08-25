using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class VerifyPaymentFailedHandler : IPreCommitDomainEventHandler<VerifyPaymentFailed>
{
    private readonly IBookingService bookings;

    public VerifyPaymentFailedHandler(IBookingService bookings)
    {
        this.bookings = bookings;
    }

    public async Task HandleAsync(VerifyPaymentFailed payment, CancellationToken ct = default)
    {
        var bookingId = await bookings.GetIdByApplicationIdAsync(payment.ApplicationId, ct);
        if (bookingId is null)
            return;

        await bookings.RecordFailedAsync(
            bookingId.Value,
            new VerifyPaymentFailedEvidence(
                payment.ApplicationId,
                payment.ProviderTransactionId,
                new FinancialOperationError(payment.Error.Code, payment.Error.Message)),
            ct);
    }
}
