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
            context.VenueReadModels.AddRange(seed.Venues.Select(v => new VenueReadModel
            {
                Id = v.Id,
                UserId = v.UserId,
                Name = v.Name,
                About = v.About,
                County = v.Address.County,
                Town = v.Address.Town,
                Location = v.Location
            }));
            await context.SaveChangesAsync(ct);
        });

        await context.ArtistReadModels.SeedIfEmptyAsync(async () =>
        {
            context.ArtistReadModels.AddRange(seed.Artists.Select(a => new ArtistReadModel
            {
                Id = a.Id,
                UserId = a.UserId,
                Name = a.Name,
                Avatar = a.Avatar,
                BannerUrl = a.BannerUrl,
                County = a.Address.County,
                Town = a.Address.Town,
                Email = a.Email,
                Genres = a.Genres.Select(g => new ArtistReadModelGenre { ArtistReadModelId = a.Id, Genre = g }).ToList()
            }));
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
