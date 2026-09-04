using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Mappers;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Domain.Lifecycle;
using Concertable.B2B.Booking.Domain.Financial;
using Concertable.B2B.Booking.Domain.ValueObjects;
using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Booking.UnitTests;

public sealed class BookingEntityTests
{
    [Fact]
    public void Create_AcceptedApplication_CopiesProvenance()
    {
        var accepted = AcceptedApplications.DoorSplit();
        var acceptance = accepted.Contract.ToBookingAcceptance();

        var booking = BookingEntity.Create(acceptance);

        Assert.Equal(accepted.Contract.OperationId, booking.OperationId);
        Assert.Equal(accepted.Contract.ApplicationId, booking.ApplicationId);
    }

    [Fact]
    public void MintContract_Contract_TakesItsExpectedFinancialOperation()
    {
        var acceptance = AcceptedApplications.DoorSplit().Contract.ToBookingAcceptance();
        var booking = BookingEntity.Create(acceptance);

        booking.MintContract(DoorSplitContract.Create(
            1,
            acceptance,
            (DoorSplitTerms)acceptance.Terms,
            "pm_123",
            new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(FinancialOperation.VerifyPayment, booking.ExpectedFinancialOperation);
    }

    [Fact]
    public void Create_MissingAcceptedApplication_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => BookingEntity.Create(null!));
}
