using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ConcertEntityTests
{
    [Fact]
    public void CreateDraft_DuplicateGenre_IsStoredOnceInInsertionOrder()
    {
        var booking = new ConfirmedBookingSnapshot(
            Guid.NewGuid(),
            1,
            2,
            3,
            4,
            5,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DealType.FlatFee,
            false,
            new DateTime(2035, 1, 1, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2035, 1, 1, 22, 0, 0, DateTimeKind.Utc),
            [Genre.Rock],
            new FlatFeeTerms(500m));

        var concert = ConcertEntity.CreateDraft(
            booking,
            "Concert",
            "About",
            [Genre.Rock, Genre.Rock, Genre.Jazz]);

        Assert.Equal([Genre.Rock, Genre.Jazz], concert.Genres);
    }
}
