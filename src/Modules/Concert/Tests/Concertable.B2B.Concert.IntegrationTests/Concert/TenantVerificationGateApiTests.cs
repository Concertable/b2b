using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

/// <summary>
/// The fail-closed tenant-verification payout gate (<c>FinishExecutor</c>): a settlement's supplier and customer
/// tenants must both be <c>Approved</c>-verified, or the concert is not transitioned and not paid (it self-heals
/// on the next sweep once verification is approved). <see cref="SeedState.UnverifiedTenant"/> is tax-compliant
/// but has no verification row, isolating this gate from <c>ConcertPayoutComplianceGateApiTests</c>' tax-
/// compliance gate.
/// </summary>
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

    private Task<ApplicationEntity> ApplicationAsync(int applicationId) =>
        fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == applicationId);

    [Fact]
    public async Task Finish_Defers_WhenPayeeArtistNotVerified_EvenThoughTaxComplianceComplete()
    {
        var concertId = fixture.SeedState.PastFlatFeeBooking.Concert!.Id;
        await RepointArtistTenantAsync(concertId, fixture.SeedState.UnverifiedTenant.Id);

        await fixture.FinishConcertAsync(concertId);

        var application = await ApplicationAsync(fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }

    [Fact]
    public async Task Finish_Settles_WhenBothTenantsVerified()
    {
        // Default seeded state: both parties are Approved-verified and tax-compliant.
        var concertId = fixture.SeedState.PastVersusBooking.Concert!.Id;
        await fixture.DeclareDoorRevenueAsync(concertId, 200m);

        await fixture.FinishConcertAsync(concertId);

        var application = await ApplicationAsync(fixture.SeedState.PastVersusApp.Id);
        Assert.Equal(LifecycleState.AwaitingSettlement, application.State);
    }
}
