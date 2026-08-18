using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.State;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class FinancialOperationEvidenceTests
{
    [Fact]
    public void FinancialOperationSucceeded_BlankProviderReference_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new VerifyPaymentSucceededEvidence(
            42,
            " "));

    [Fact]
    public void AcceptanceFinancialOperationRejected_DoesNotRequireProviderReference()
    {
        var rejected = new AcceptanceFinancialOperationRejected(
            Guid.NewGuid(),
            42,
            FinancialOperation.CaptureEscrow,
            new FinancialOperationError("capture_failed", "Capture failed"));

        Assert.Equal(42, rejected.BookingId);
        Assert.Equal("capture_failed", rejected.Error.Code);
    }

    [Fact]
    public void FinancialOperationError_BlankCode_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new FinancialOperationError(" ", "Declined"));

    [Fact]
    public void FinancialOperationError_BlankMessage_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new FinancialOperationError("card_declined", " "));
}
