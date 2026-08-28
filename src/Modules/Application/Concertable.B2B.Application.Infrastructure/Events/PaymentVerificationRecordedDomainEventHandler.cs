using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Domain.ValueObjects;
using Concertable.B2B.Booking.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Events;

internal sealed class PaymentVerificationRecordedDomainEventHandler(
    IBookingModule bookingModule) : IPreCommitDomainEventHandler<PaymentVerificationRecordedDomainEvent>
{
    public Task HandleAsync(PaymentVerificationRecordedDomainEvent @event, CancellationToken ct = default) =>
        bookingModule.RecordPaymentVerificationAsync(@event.Verification switch
        {
            SuccessfulPaymentVerification succeeded =>
                new SuccessfulBookingPaymentVerification(
                    succeeded.ApplicationId,
                    succeeded.ProviderTransactionId),
            FailedPaymentVerification failed =>
                new FailedBookingPaymentVerification(
                    failed.ApplicationId,
                    failed.ProviderTransactionId,
                    failed.Failure.Code,
                    failed.Failure.Message),
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, null)
        }, ct);
}
