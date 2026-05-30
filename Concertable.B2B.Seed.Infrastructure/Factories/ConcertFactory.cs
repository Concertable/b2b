using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;
using Concertable.Kernel;
using static Concertable.Seed.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class ConcertFactory
{
    public static ConcertEntity Create(
        int id,
        int bookingId,
        int artistId,
        int venueId,
        DateRange period,
        string name,
        string about,
        IEnumerable<Genre> genres,
        decimal price,
        int totalTickets,
        DateTime? datePosted)
    {
        var concert = ConcertEntity
            .CreateDraft(bookingId, artistId, venueId, period, name, about, genres)
            .With(nameof(ConcertEntity.Id), id)
            .With(nameof(ConcertEntity.Price), price)
            .With(nameof(ConcertEntity.TotalTickets), totalTickets);
        if (datePosted is not null)
            concert.Post(concert.Name, concert.About, concert.Price, concert.TotalTickets, datePosted.Value);
        return concert;
    }
}
