using Concertable.B2B.Booking.Application.Models;
using Concertable.B2B.Booking.Domain.State;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class FinancialOperationEvidenceTests
{
    [Fact]
    public void FinancialOperationSucceeded_BlankProviderReference_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new FinancialOperationSucceeded(
            42,
            FinancialOperation.VerifyPayment,
            " "));

    [Fact]
    public void FinancialOperationError_BlankCode_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new FinancialOperationError(" ", "Declined"));

    [Fact]
    public void FinancialOperationError_BlankMessage_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new FinancialOperationError("card_declined", " "));
}
