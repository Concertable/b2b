using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.State;
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
        var booking = await bookings.GetByApplicationIdAsync(payment.ApplicationId, ct);
        if (booking is null)
            return;

        await bookings.RecordFailedAsync(
            booking.Id,
            new FinancialOperationFailed(
                payment.ApplicationId,
                FinancialOperation.VerifyPayment,
                payment.ProviderTransactionId,
                new FinancialOperationError(payment.Error.Code, payment.Error.Message)),
            ct);
    }
}
