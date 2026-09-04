using Concertable.B2B.Booking.Contracts;
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

    private static ConcertEntity CreateConcert()
    {
        var booking = new ConfirmedBooking(
            Guid.NewGuid(),
            1,
            2,
            3,
            4,
            5,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DealType.DoorSplit,
            true,
            new DateTime(2026, 8, 10, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 10, 22, 0, 0, DateTimeKind.Utc),
            [Genre.Rock],
            new DoorSplitTerms(50m));
        return ConcertEntity.CreateDraft(
            booking,
            "Concert",
            "About",
            [Genre.Rock]);
    }
}
