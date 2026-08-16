using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Errors;
using Concertable.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests.Domain;

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
        var application = StandardApplication.Create(
            1,
            2,
            DealType.DoorSplit,
            Guid.NewGuid(),
            Guid.NewGuid());
        var booking = StandardBooking.Create(application);
        return ConcertEntity.CreateDraft(
            booking,
            1,
            2,
            new DateRange(
                new DateTime(2026, 8, 10, 19, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 8, 10, 22, 0, 0, DateTimeKind.Utc)),
            "Concert",
            "About",
            [Genre.Rock]);
    }
}
