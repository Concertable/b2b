using Bogus;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;
using Concertable.Kernel;
using static Concertable.Seeding.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seeding.Fakers;

public static class ConcertFaker
{
    private static readonly Faker faker = new();

    public static ConcertEntity Create(
        int id,
        int bookingId,
        string name,
        decimal price,
        int totalTickets,
        int artistId,
        int venueId,
        DateRange period,
        IEnumerable<Genre>? genres = null)
        => ConcertEntity
            .CreateDraft(bookingId, artistId, venueId, new DateRange(period.Start, period.End), name, faker.Lorem.Paragraph(7), genres ?? [])
            .With(nameof(ConcertEntity.Id), id)
            .With(nameof(ConcertEntity.Price), price)
            .With(nameof(ConcertEntity.TotalTickets), totalTickets);

    public static ConcertEntity Post(
        int id,
        int bookingId,
        string name,
        decimal price,
        int totalTickets,
        int artistId,
        int venueId,
        DateRange period,
        DateTime datePosted,
        IEnumerable<Genre>? genres = null)
    {
        var concert = Create(id, bookingId, name, price, totalTickets, artistId, venueId, period, genres);
        concert.Post(concert.Name, concert.About, concert.Price, concert.TotalTickets, datePosted);
        return concert;
    }

    public static ConcertEntity FromSeedFixture(ConcertChangedEvent e, int bookingId)
    {
        var concert = ConcertEntity
            .CreateDraft(
                bookingId: bookingId,
                artistId:  e.ArtistId,
                venueId:   e.VenueId,
                period:    e.Period,
                name:      e.Name,
                about:     e.About,
                genres:    e.Genres)
            .With(nameof(ConcertEntity.Id), e.ConcertId)
            .With(nameof(ConcertEntity.Price), e.Price)
            .With(nameof(ConcertEntity.TotalTickets), e.TotalTickets);
        if (e.DatePosted is not null)
            concert.Post(concert.Name, concert.About, concert.Price, concert.TotalTickets, e.DatePosted.Value);
        return concert;
    }
}
