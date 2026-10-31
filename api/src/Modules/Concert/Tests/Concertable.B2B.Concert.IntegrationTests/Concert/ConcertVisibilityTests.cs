using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]
public sealed class ConcertVisibilityTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ConcertVisibilityTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync()
    {
        fixture.DetachOutput();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Filter_FollowsPartyAssignmentAndShareRoutes()
    {
        using var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        var privileged = services.GetRequiredService<ConcertPrivilegedDbContext>();
        var seeded = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var concert = await privileged.Concerts.SingleAsync(candidate => candidate.Id == seeded.Id);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var assignee = Guid.NewGuid();
        var recipientTenantId = Guid.NewGuid();
        var expiredRecipientTenantId = Guid.NewGuid();
        Assert.True(concert.AssignMember(concert.VenueTenantId, Guid.NewGuid(), assignee, now).IsSuccess);
        Assert.True(concert.ShareSummary(
            concert.VenueTenantId, Guid.NewGuid(), recipientTenantId, null, now, null).IsSuccess);
        Assert.True(concert.ShareSummary(
            concert.VenueTenantId, Guid.NewGuid(), expiredRecipientTenantId, null,
            now.AddDays(-2), now.AddDays(-1)).IsSuccess);
        await privileged.SaveChangesAsync();

        Assert.True(await IsVisibleAsync(services, seeded.Id, Actor(concert.VenueTenantId, Guid.NewGuid(), ResourceAccess.All)));
        Assert.True(await IsVisibleAsync(services, seeded.Id, Actor(concert.ArtistTenantId, Guid.NewGuid(), ResourceAccess.All)));
        Assert.False(await IsVisibleAsync(services, seeded.Id, Actor(concert.VenueTenantId, Guid.NewGuid(), ResourceAccess.Assigned)));
        Assert.True(await IsVisibleAsync(services, seeded.Id, Actor(concert.VenueTenantId, assignee, ResourceAccess.Assigned)));
        Assert.False(await IsVisibleAsync(services, seeded.Id, Actor(concert.VenueTenantId, assignee, ResourceAccess.None)));
        Assert.True(await IsVisibleAsync(services, seeded.Id, Actor(recipientTenantId, Guid.NewGuid(), ResourceAccess.All)));
        Assert.False(await IsVisibleAsync(services, seeded.Id, Actor(recipientTenantId, Guid.NewGuid(), ResourceAccess.Assigned)));
        Assert.False(await IsVisibleAsync(services, seeded.Id, Actor(expiredRecipientTenantId, Guid.NewGuid(), ResourceAccess.All)));
        Assert.False(await IsVisibleAsync(services, seeded.Id, Actor(Guid.NewGuid(), Guid.NewGuid(), ResourceAccess.All)));
    }

    private static MembershipSnapshot Actor(Guid tenantId, Guid membershipId, ResourceAccess access) =>
        new(membershipId, tenantId, Guid.NewGuid(), 1, 1,
            EffectivePermissions.From([KeyValuePair.Create(TenantPermission.OperationsView, access)]));

    private static async Task<bool> IsVisibleAsync(IServiceProvider services, int concertId, MembershipSnapshot actor)
    {
        await using var context = new ConcertDbContext(
            services.GetRequiredService<DbContextOptions<ConcertDbContext>>(),
            services.GetRequiredService<IOptions<OutboxOptions>>(),
            services.GetRequiredService<ConcertConfigurationProvider>(),
            services.GetRequiredService<ITenantContext>(),
            new FixedAuthorizationContext(actor),
            TimeProvider.System);
        return await context.Concerts.AnyAsync(concert => concert.Id == concertId);
    }

    private sealed class FixedAuthorizationContext(MembershipSnapshot actor) : IAuthorizationContext
    {
        public MembershipSnapshot? Membership => actor;
    }
}
