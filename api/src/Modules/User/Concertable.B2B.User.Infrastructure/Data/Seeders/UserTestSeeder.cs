using Concertable.Auth.Contracts;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.User.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Shared;
using Concertable.B2B.Seed.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Data.Seeders;

internal sealed class UserTestSeeder : ITestSeeder
{
    public int Order => 0;

    private readonly UserDbContext context;
    private readonly SeedState seedData;
    private readonly CredentialRegisteredHandler credentialRegisteredHandler;

    public UserTestSeeder(
        UserDbContext context,
        SeedState seedData,
        IEnumerable<IIntegrationEventHandler<CredentialRegisteredEvent>> handlers)
    {
        this.context = context;
        this.seedData = seedData;
        credentialRegisteredHandler = handlers.OfType<CredentialRegisteredHandler>().Single();
    }

    public Task MigrateAsync(CancellationToken ct = default) => context.Database.MigrateAsync(ct);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var baselineIds = seedData.Users.Select(user => user.Id).ToHashSet();
        var transientUsers = await context.Users
            .Where(user => !baselineIds.Contains(user.Id))
            .ToListAsync(ct);
        if (transientUsers.Count > 0)
        {
            context.Users.RemoveRange(transientUsers);
            await context.SaveChangesAsync(ct);
        }

        var existingIds = await context.Users
            .Where(user => baselineIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToHashSetAsync(ct);
        foreach (var user in seedData.Users.Where(user => !existingIds.Contains(user.Id)))
        {
            await credentialRegisteredHandler.HandleAsync(
                new CredentialRegisteredEvent(
                    user.Id,
                    user.Email,
                    InteractiveClientInfo.Get(InteractiveClient.VenueBrowser).Id),
                MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow),
                ct);
        }
    }
}
