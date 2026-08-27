using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Financial;

namespace Concertable.B2B.Booking.Infrastructure.Services;

internal static class VerifyPaymentAdvancer
{
    public static Task AdvanceAsync(
        IBookingService bookings,
        int bookingId,
        VerifyPayment? verification,
        CancellationToken ct) => verification switch
    {
        VerifyPaymentSucceeded succeeded => bookings.RecordSucceededAsync(
            bookingId,
            new VerifyPaymentSucceededEvidence(
                succeeded.ApplicationId,
                succeeded.ProviderTransactionId),
            ct),
        VerifyPaymentFailed failed => bookings.RecordFailedAsync(
            bookingId,
            new VerifyPaymentFailedEvidence(
                failed.ApplicationId,
                failed.ProviderTransactionId,
                new FinancialOperationError(failed.Error.Code, failed.Error.Message)),
            ct),
        null => Task.CompletedTask,
        _ => throw new ArgumentOutOfRangeException(nameof(verification), verification, null)
    };
}
