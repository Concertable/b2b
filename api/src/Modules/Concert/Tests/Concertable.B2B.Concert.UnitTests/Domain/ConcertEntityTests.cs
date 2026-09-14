using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.Contracts.Enums;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class ConcertEntityTests
{
    [Fact]
    public void CreateDraft_DuplicateGenre_IsStoredOnceInInsertionOrder()
    {
        var booking = ConfirmedBookings.FlatFee();

        var concert = ConcertEntity.CreateDraft(
            booking,
            new ConcertDraft("Concert", "About", [Genre.Rock, Genre.Rock, Genre.Jazz]));

        Assert.Equal([Genre.Rock, Genre.Jazz], concert.Genres);
    }
}
