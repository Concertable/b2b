using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]
public sealed class TenantVerificationGateApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public TenantVerificationGateApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private async Task RepointArtistTenantAsync(int concertId, Guid artistTenantId)
    {
        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
        await context.Concerts.Where(c => c.Id == concertId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ArtistTenantId, artistTenantId));
    }

    private Task<ConcertEntity> ConcertAsync(int concertId) =>
        fixture.Concerts.FirstAsync(concert => concert.Id == concertId);

    [Fact]
    public async Task Finish_Defers_WhenPayeeArtistNotVerified_EvenThoughTaxComplianceComplete()
    {
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking).Id;
        await RepointArtistTenantAsync(concertId, fixture.SeedState.UnverifiedTenant.Id);

        await fixture.FinishConcertAsync(concertId);

        var concert = await ConcertAsync(concertId);
        Assert.Equal(ConcertState.Draft, concert.State);
    }

    [Fact]
    public async Task Finish_Settles_WhenBothTenantsVerified()
    {
        // Default seeded state: both parties are Approved-verified and tax-compliant.
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastVersusBooking).Id;
        await fixture.DeclareDoorRevenueAsync(concertId, 200m);

        await fixture.FinishConcertAsync(concertId);

        var concert = await ConcertAsync(concertId);
        Assert.Equal(ConcertState.AwaitingSettlement, concert.State);
    }
}
