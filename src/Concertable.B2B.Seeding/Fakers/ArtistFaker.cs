using Bogus;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.Artist.Domain;
using Concertable.Contracts;
using Concertable.Kernel;
using NetTopologySuite.Geometries;
using static Concertable.Seeding.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seeding.Fakers;

public static class ArtistFaker
{
    private static readonly Faker faker = new();

    public static ArtistEntity Create(
        int id,
        Guid userId,
        string name,
        string bannerUrl,
        string avatar,
        Point location,
        Address address,
        string email,
        IEnumerable<Genre> genres)
        => ArtistEntity
            .Create(userId, name, faker.Lorem.Paragraph(7), bannerUrl, avatar, location, address, email, genres)
            .With(nameof(ArtistEntity.Id), id);

    public static ArtistEntity FromSeedFixture(ArtistChangedEvent e)
        => ArtistEntity
            .Create(
                userId:    e.UserId,
                name:      e.Name,
                about:     e.About,
                bannerUrl: e.BannerUrl,
                avatar:    e.Avatar,
                location:  new Point(e.Longitude, e.Latitude) { SRID = 4326 },
                address:   new Address(e.County, e.Town),
                email:     e.Email,
                genres:    e.Genres)
            .With(nameof(ArtistEntity.Id), e.ArtistId);
}
