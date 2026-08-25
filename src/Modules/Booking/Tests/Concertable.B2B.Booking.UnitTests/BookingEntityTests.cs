using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class BookingEntityTests
{
    [Fact]
    public void Create_AcceptedApplication_CopiesProvenanceAndExpectedOperation()
    {
        var accepted = AcceptedApplications.DoorSplit();

        var booking = DeferredBooking.Create(accepted, accepted.PaymentMethodId);

        Assert.Equal(accepted.OperationId, booking.OperationId);
        Assert.Equal(accepted.ApplicationId, booking.ApplicationId);
        Assert.Equal(FinancialOperation.VerifyPayment, booking.ExpectedFinancialOperation);
    }

    [Fact]
    public void Create_MissingAcceptedApplication_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => DeferredBooking.Create(null!, "pm_123"));
}
