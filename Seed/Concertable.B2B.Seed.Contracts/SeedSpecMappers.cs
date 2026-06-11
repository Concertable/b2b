using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Seed.Contracts.Specs;
using Concertable.B2B.Venue.Contracts.Events;
using Concertable.Seed.Identity;

namespace Concertable.B2B.Seed.Contracts;

public static class SeedSpecMappers
{
    public static VenueChangedEvent ToChangedEvent(this VenueSeedSpec spec) => new(
        spec.VenueId, spec.UserId, spec.Name, spec.About, spec.Avatar, spec.BannerUrl,
        spec.County, spec.Town, spec.Latitude, spec.Longitude, spec.Email);

    public static ArtistChangedEvent ToChangedEvent(this ArtistSeedSpec spec) => new(
        spec.ArtistId, spec.UserId, spec.Name, spec.About, spec.Avatar, spec.BannerUrl,
        spec.County, spec.Town, spec.Latitude, spec.Longitude, spec.Email, spec.Genres,
        TenantSeedIds.For(spec.UserId));

    public static ConcertChangedEvent ToChangedEvent(this ConcertSeedSpec spec) => new(
        spec.ConcertId, spec.Name, spec.About, spec.Avatar, spec.BannerUrl,
        spec.TotalTickets, spec.AvailableTickets, spec.Price, spec.Period, spec.DatePosted,
        spec.ArtistId, spec.ArtistName, spec.VenueId, spec.VenueName,
        spec.Latitude, spec.Longitude, spec.Genres, spec.PayeeUserId, spec.PayeeOwnerId);
}
