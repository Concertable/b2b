using Concertable.Seed.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Data.Seeders;

internal sealed class UserDevSeeder : IDevSeeder
{
    public int Order => 0;

    private readonly UserDbContext context;

    public UserDevSeeder(UserDbContext context)
    {
        this.context = context;
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public Task SeedAsync(CancellationToken ct = default) => Task.CompletedTask;
}
