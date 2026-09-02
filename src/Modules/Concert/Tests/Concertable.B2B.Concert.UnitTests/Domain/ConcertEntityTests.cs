using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;
using Xunit;

namespace Concertable.B2B.Concert.UnitTests.Domain;

public sealed class ConcertEntityTests
{
    [Fact]
    public void CreateDraft_DuplicateGenre_IsStoredOnceInInsertionOrder()
    {
        var application = StandardApplication.Create(1, 2, DealType.FlatFee, Guid.NewGuid(), Guid.NewGuid());
        var booking = StandardBooking.Create(application);
        var period = new DateRange(
            new DateTime(2035, 1, 1, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2035, 1, 1, 22, 0, 0, DateTimeKind.Utc));

        var concert = ConcertEntity.CreateDraft(
            booking, 1, 2, period, "Concert", "About", [Genre.Rock, Genre.Rock, Genre.Jazz]);

        Assert.Equal([Genre.Rock, Genre.Jazz], concert.Genres);
    }
}
