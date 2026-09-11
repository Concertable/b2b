using Concertable.Seed.Shared;
using Concertable.B2B.Admin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Admin.Infrastructure.Data.Seeders;

internal sealed class AdminDevSeeder : IDevSeeder
{
    public int Order => 1;

    private readonly AdminDbContext context;

    public AdminDevSeeder(AdminDbContext context)
    {
        this.context = context;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}
