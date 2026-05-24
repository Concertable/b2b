using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using Concertable.Seeding.Fakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Contracts;
using Concertable.Kernel.Geometry;
using Concertable.Kernel.Services.Geometry;

namespace Concertable.B2B.Artist.Infrastructure.Data.Seeders;

internal class ArtistDevSeeder : IDevSeeder
{
    public int Order => 1;

    private readonly ArtistDbContext context;
    private readonly SeedData seed;
    private readonly IGeometryProvider geometryProvider;
    private readonly ILocationFaker locationFaker;

    public ArtistDevSeeder(
        ArtistDbContext context,
        SeedData seed,
        [FromKeyedServices(GeometryProviderType.Geographic)] IGeometryProvider geometryProvider,
        ILocationFaker locationFaker)
    {
        this.context = context;
        this.seed = seed;
        this.geometryProvider = geometryProvider;
        this.locationFaker = locationFaker;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var artistManagerIds = seed.ArtistManagerIds;

        await context.Artists.SeedIfEmptyAsync(async () =>
        {
            var bands = new (string Name, string Banner, Genre[] Genres)[]
            {
                ("The Rockers", "rockers.jpg", [Genre.Rock, Genre.Pop, Genre.Jazz]),
                ("Indie Vibes", "indievibes.jpg", [Genre.Rock, Genre.Electronic, Genre.HipHop]),
                ("Electronic Pulse", "electronicpulse.jpg", [Genre.Electronic, Genre.Jazz]),
                ("Hip-Hop Flow", "hiphopflow.jpg", [Genre.HipHop]),
                ("Jazz Masters", "jazzmaster.jpg", [Genre.Indie, Genre.Jazz]),
                ("Always Punks", "alwayspunks.jpg", [Genre.Rock, Genre.Indie]),
                ("The Hollow Frequencies", "hollowfrequencies.jpg", [Genre.Pop]),
                ("Neon Foxes", "neonfoxes.jpg", [Genre.HipHop, Genre.Pop]),
                ("Velvet Static", "velvetstatic.jpg", [Genre.Electronic, Genre.Jazz]),
                ("Echo Bloom", "echobloom.jpg", [Genre.Rock, Genre.DnB]),
                ("The Wild Chords", "wildchords.jpg", [Genre.Indie, Genre.Rock]),
                ("Glitch & Glow", "glitchandglow.jpg", [Genre.Pop]),
                ("Sonic Mirage", "sonicmirage.jpg", [Genre.Indie, Genre.Electronic]),
                ("Neon Echoes", "neonechoes.jpg", [Genre.HipHop]),
                ("Dreamwave Collective", "dreamwavecollective.jpg", [Genre.DnB]),
                ("Synth Pulse", "synthpulse.jpg", [Genre.Rock]),
                ("The Brass Poets", "brasspoets.jpg", [Genre.Jazz]),
                ("Groove Alchemy", "groovealchemy.jpg", [Genre.Indie]),
                ("Velvet Rhymes", "velvetrhymes.jpg", [Genre.HipHop]),
                ("The Lo-Fi Syndicate", "lofisyndicate.jpg", [Genre.DnB]),
                ("Beats & Blue Notes", "beatsbluenotes.jpg", [Genre.House]),
                ("Bass Pilots", "basspilots.jpg", [Genre.Rock]),
                ("The Digital Prophets", "digitalprophets.jpg", [Genre.Electronic]),
                ("Neon Bass Theory", "neonbasstheory.jpg", [Genre.Indie]),
                ("Wavelength 303", "wavelength303.jpg", [Genre.Pop]),
                ("Gravity Loops", "gravityloops.jpg", [Genre.Rock]),
                ("The Golden Reverie", "goldenreverie.jpg", [Genre.House]),
                ("Fable Sound", "fablesound.jpg", [Genre.Electronic]),
                ("Moonlight Static", "moonlightstatic.jpg", [Genre.DnB]),
                ("The Chromatics", "thechromatics.jpg", [Genre.Jazz]),
                ("Echo Reverberation", "echoreverberation.jpg", [Genre.Indie]),
                ("Midnight Reverie", "midnightreverie.jpg", [Genre.Rock]),
                ("Static Wolves", "staticwolves.jpg", [Genre.HipHop]),
                ("Echo Collapse", "echocollapse.jpg", [Genre.Pop]),
                ("Violet Sundown", "violetsundown.jpg", [Genre.House])
            };

            var artists = bands.Select((b, i) =>
            {
                var loc = locationFaker.Next();
                return ArtistFaker.GetFaker(
                    i + 1,
                    artistManagerIds[i],
                    b.Name,
                    b.Banner,
                    "avatar.jpg",
                    geometryProvider.CreatePoint(loc.Latitude, loc.Longitude),
                    new Address(loc.County, loc.Town),
                    $"{b.Name.ToLowerInvariant().Replace(" ", "")}@test.com",
                    b.Genres).Generate();
            }).ToArray();

            context.Artists.AddRange(artists);
            await context.SaveChangesAsync(ct);
        });
    }
}
