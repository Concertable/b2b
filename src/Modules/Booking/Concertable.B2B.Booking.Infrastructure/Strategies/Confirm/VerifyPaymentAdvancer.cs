using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Booking.Infrastructure.Strategies;

internal static class VerifyPaymentAdvancer
{
    public static void Advance(BookingEntity booking, VerifyPayment verification, DealTerms terms)
    {
        switch (verification)
        {
            case VerifyPaymentSucceeded succeeded:
                if (booking.RecordFinancialConfirmation(succeeded.ProviderTransactionId, terms)
                    .TryGetError(out var confirmationError))
                    throw new InvalidOperationException(
                        $"Booking cannot confirm from {confirmationError.Current}.");
                break;
            case VerifyPaymentFailed failed:
                if (booking.RecordFinancialFailure(
                    failed.ProviderTransactionId,
                    failed.Error.Code,
                    failed.Error.Message).TryGetError(out var failureError))
                    throw new InvalidOperationException(
                        $"Booking cannot record confirmation failure from {failureError.Current}.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(verification), verification, null);
        }
    }
}
