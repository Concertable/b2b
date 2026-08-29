using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Domain.ValueObjects;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class BookingEntityTests
{
    [Fact]
    public void Create_AcceptedApplication_CopiesProvenanceAndExpectedOperation()
    {
        var accepted = AcceptedApplications.DoorSplit();
        var acceptance = (DeferredBookingAcceptance)accepted.ToBookingAcceptance();

        var booking = DeferredBooking.Create(acceptance);

        Assert.Equal(accepted.OperationId, booking.OperationId);
        Assert.Equal(accepted.ApplicationId, booking.ApplicationId);
        Assert.Equal(FinancialOperation.VerifyPayment, booking.ExpectedFinancialOperation);
    }

    [Fact]
    public void Create_MissingAcceptedApplication_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => DeferredBooking.Create(null!));
}
