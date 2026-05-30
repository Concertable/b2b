using Concertable.B2B.Artist.Domain;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Seeding.Fixture.Specs;
using Concertable.B2B.Venue.Domain;
using Concertable.Kernel;
using NetTopologySuite.Geometries;
using static Concertable.Seeding.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seeding;

public static class SeedSpecMappers
{
    public static VenueEntity ToEntity(this VenueSeedSpec spec)
    {
        var venue = VenueEntity
            .Create(
                userId:    spec.UserId,
                name:      spec.Name,
                about:     spec.About,
                bannerUrl: spec.BannerUrl,
                avatar:    spec.Avatar,
                location:  new Point(spec.Longitude, spec.Latitude) { SRID = 4326 },
                address:   new Address(spec.County, spec.Town),
                email:     spec.Email)
            .With(nameof(VenueEntity.Id), spec.VenueId);
        venue.Approve();
        return venue;
    }

    public static ArtistEntity ToEntity(this ArtistSeedSpec spec)
        => ArtistEntity
            .Create(
                userId:    spec.UserId,
                name:      spec.Name,
                about:     spec.About,
                bannerUrl: spec.BannerUrl,
                avatar:    spec.Avatar,
                location:  new Point(spec.Longitude, spec.Latitude) { SRID = 4326 },
                address:   new Address(spec.County, spec.Town),
                email:     spec.Email,
                genres:    spec.Genres)
            .With(nameof(ArtistEntity.Id), spec.ArtistId);

    public static ConcertEntity ToEntity(this ConcertSeedSpec spec, int bookingId)
    {
        var concert = ConcertEntity
            .CreateDraft(
                bookingId: bookingId,
                artistId:  spec.ArtistId,
                venueId:   spec.VenueId,
                period:    spec.Period,
                name:      spec.Name,
                about:     spec.About,
                genres:    spec.Genres)
            .With(nameof(ConcertEntity.Id), spec.ConcertId)
            .With(nameof(ConcertEntity.Price), spec.Price)
            .With(nameof(ConcertEntity.TotalTickets), spec.TotalTickets);
        if (spec.DatePosted is not null)
            concert.Post(concert.Name, concert.About, concert.Price, concert.TotalTickets, spec.DatePosted.Value);
        return concert;
    }
}
