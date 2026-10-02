using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests;

[Collection("Integration")]
public sealed class ObligationCheckerTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ObligationCheckerTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        this.fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => this.fixture.ResetAsync();
    public Task DisposeAsync() { this.fixture.DetachOutput(); return Task.CompletedTask; }

    [Theory]
    [InlineData(ConcertState.Draft, true)]
    [InlineData(ConcertState.Complete, false)]
    [InlineData(ConcertState.Cancelled, false)]
    public async Task HandedOffConcert_IsLiveUntilFinanciallyTerminal(ConcertState state, bool expected)
    {
        var concert = this.fixture.SeedState.Concerts[0];
        var tenantIds = new HashSet<Guid> { concert.VenueTenantId };
        var live = await this.fixture.Services.RunScopedAsync(async sp =>
        {
            var context = sp.GetRequiredService<ConcertPrivilegedDbContext>();
            await context.Concerts
                .Where(value => tenantIds.Contains(value.VenueTenantId) || tenantIds.Contains(value.ArtistTenantId))
                .ExecuteUpdateAsync(setters => setters.SetProperty(value => value.State, ConcertState.Complete));
            await context.SelfBillingAgreements
                .Where(value => tenantIds.Contains(value.TenantId))
                .ExecuteDeleteAsync();
            await context.Concerts.Where(value => value.Id == concert.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(value => value.State, state));
            return await sp.GetRequiredService<IConcertModule>().HasLiveObligationsByTenantIdsAsync(tenantIds);
        });

        Assert.Equal(expected, live);
    }
}
