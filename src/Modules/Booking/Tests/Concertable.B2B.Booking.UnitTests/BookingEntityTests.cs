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
        var snapshot = accepted.Snapshot;

        var booking = BookingEntity.Create(snapshot);

        Assert.Equal(snapshot.OperationId, booking.OperationId);
        Assert.Equal(snapshot.Application.Id, booking.ApplicationId);
    }

    [Fact]
    public void MintContract_Contract_TakesItsExpectedFinancialOperation()
    {
        var snapshot = AcceptedApplications.DoorSplit().Snapshot;
        var booking = BookingEntity.Create(snapshot);

        booking.MintContract(DoorSplitContract.Create(
            1,
            snapshot,
            (DoorSplitTerms)snapshot.Contract.Terms,
            new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));

        Assert.Equal(FinancialOperation.VerifyPayment, booking.ExpectedFinancialOperation);
    }

    [Fact]
    public void Create_MissingAcceptedApplication_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => BookingEntity.Create(null!));
}
