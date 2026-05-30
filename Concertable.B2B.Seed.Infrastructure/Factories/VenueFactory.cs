using Concertable.B2B.Venue.Domain;
using Concertable.Kernel;
using NetTopologySuite.Geometries;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class VenueFactory
{
    public static VenueEntity Create(
        int id,
        Guid userId,
        string name,
        string about,
        string bannerUrl,
        string avatar,
        Point location,
        Address address,
        string email)
    {
        var venue = VenueEntity
            .Create(userId, name, about, bannerUrl, avatar, location, address, email)
            .With(nameof(VenueEntity.Id), id);
        venue.Approve();
        return venue;
    }
}
