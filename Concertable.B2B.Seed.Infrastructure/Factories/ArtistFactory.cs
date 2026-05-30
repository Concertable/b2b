using Concertable.B2B.Artist.Domain;
using Concertable.Contracts;
using Concertable.Kernel;
using NetTopologySuite.Geometries;
using static Concertable.Seed.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class ArtistFactory
{
    public static ArtistEntity Create(
        int id,
        Guid userId,
        string name,
        string about,
        string bannerUrl,
        string avatar,
        Point location,
        Address address,
        string email,
        IEnumerable<Genre> genres)
        => ArtistEntity
            .Create(userId, name, about, bannerUrl, avatar, location, address, email, genres)
            .With(nameof(ArtistEntity.Id), id);
}
