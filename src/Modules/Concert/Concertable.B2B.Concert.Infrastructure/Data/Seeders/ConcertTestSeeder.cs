using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data.Seeders;

internal class ConcertTestSeeder : ITestSeeder
{
    public int Order => 4;

    private readonly ConcertDbContext context;
    private readonly SeedState seed;

    public ConcertTestSeeder(ConcertDbContext context, SeedState seed)
    {
        this.context = context;
        this.seed = seed;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await context.VenueReadModels.SeedIfEmptyAsync(async () =>
        {
            context.VenueReadModels.Add(new VenueReadModel
            {
                Id = seed.Venue.Id,
                UserId = seed.Venue.UserId,
                Name = seed.Venue.Name,
                About = seed.Venue.About,
                County = seed.Venue.Address.County,
                Town = seed.Venue.Address.Town,
                Location = seed.Venue.Location
            });
            await context.SaveChangesAsync(ct);
        });

        await context.ArtistReadModels.SeedIfEmptyAsync(async () =>
        {
            context.ArtistReadModels.Add(new ArtistReadModel
            {
                Id = seed.Artist.Id,
                UserId = seed.Artist.UserId,
                Name = seed.Artist.Name,
                Avatar = seed.Artist.Avatar,
                BannerUrl = seed.Artist.BannerUrl,
                County = seed.Artist.Address.County,
                Town = seed.Artist.Address.Town,
                Email = seed.Artist.Email,
                Genres = seed.Artist.Genres.Select(g => new ArtistReadModelGenre { ArtistReadModelId = seed.Artist.Id, Genre = g }).ToList()
            });
            await context.SaveChangesAsync(ct);
        });

        await context.Opportunities.SeedIfEmptyAsync(async () =>
        {
            context.Opportunities.AddRange(seed.Opportunities);
            await context.SaveChangesAsync(ct);
        });

        await context.Applications.SeedIfEmptyAsync(async () =>
        {
            context.Applications.AddRange(seed.Applications);
            await context.SaveChangesAsync(ct);

            context.Concerts.AddRange(seed.Concerts);
            await context.SaveChangesAsync(ct);
        });
    }
}
