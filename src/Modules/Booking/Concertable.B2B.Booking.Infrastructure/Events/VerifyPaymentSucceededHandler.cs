using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class VerifyPaymentSucceededHandler : IPreCommitDomainEventHandler<VerifyPaymentSucceeded>
{
    private readonly IBookingService bookings;

    public VerifyPaymentSucceededHandler(IBookingService bookings)
    {
        this.bookings = bookings;
    }

    public async Task HandleAsync(VerifyPaymentSucceeded payment, CancellationToken ct = default)
    {
        var bookingId = await bookings.GetIdByApplicationIdAsync(payment.ApplicationId, ct);
        if (bookingId is null)
            return;

        await bookings.RecordSucceededAsync(
            bookingId.Value,
            new VerifyPaymentSucceededEvidence(
                payment.ApplicationId,
                payment.ProviderTransactionId),
            ct);
    }
}
