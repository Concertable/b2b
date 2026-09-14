using Concertable.B2B.Artist.Domain.Entities;
using Concertable.Contracts.Enums;
using Concertable.Kernel.ValueObjects;
using NetTopologySuite.Geometries;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

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
        IReadOnlyCollection<Genre> genres)
        => ArtistEntity
            .Create(userId, name, about, bannerUrl, avatar, location, address, email, genres)
            .Match(
                artist => artist.WithId(id),
                _ => throw new InvalidOperationException($"Seed artist {id} is invalid."));
}
