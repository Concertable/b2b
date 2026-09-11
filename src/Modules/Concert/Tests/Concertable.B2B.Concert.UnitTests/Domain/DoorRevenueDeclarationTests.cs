using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Errors;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class DoorRevenueDeclarationTests
{
    [Fact]
    public void DeclareDoorRevenue_NegativeValue_ReturnsTypedFailureWithoutMutation()
    {
        var concert = CreateConcert();

        var result = concert.DeclareDoorRevenue(-0.01m);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<DoorRevenueDeclarationError.NegativeRevenue>(error);
        Assert.Null(concert.DoorRevenue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(125.50)]
    public void DeclareDoorRevenue_NonNegativeValue_RecordsRevenue(decimal doorRevenue)
    {
        var concert = CreateConcert();

        var result = concert.DeclareDoorRevenue(doorRevenue);

        Assert.True(result.IsSuccess);
        Assert.Equal(doorRevenue, concert.DoorRevenue);
    }

    private static DoorRevenueConcert CreateConcert()
    {
        var booking = ConfirmedBookings.DoorSplit(50m);
        return (DoorRevenueConcert)ConcertEntity.CreateDraft(
            booking, new ConcertDraft("Concert", "About", [Genre.Rock]));
    }
}
