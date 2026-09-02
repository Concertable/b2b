using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.Kernel;

namespace Concertable.B2B.Booking.Infrastructure.Events;

internal sealed class VerifyPaymentFailedHandler : IPreCommitDomainEventHandler<VerifyPaymentFailed>
{
    private readonly IBookingService bookingService;

    public VerifyPaymentFailedHandler(IBookingService bookingService)
    {
        this.bookingService = bookingService;
    }

    public async Task HandleAsync(VerifyPaymentFailed payment, CancellationToken ct = default)
    {
        var bookingId = await bookingService.GetIdByApplicationIdAsync(payment.ApplicationId, ct);
        if (bookingId is null)
            return;

        await bookingService.RecordFailedAsync(
            bookingId.Value,
            new VerifyPaymentFailedEvidence(
                payment.ApplicationId,
                payment.ProviderTransactionId,
                new FinancialOperationError(payment.Error.Code, payment.Error.Message)),
            ct);
    }
}
