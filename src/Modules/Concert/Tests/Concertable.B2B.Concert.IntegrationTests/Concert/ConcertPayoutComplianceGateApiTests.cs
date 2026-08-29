using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

/// <summary>
/// The fail-closed tax-compliance payout gate (<c>FinishExecutor</c>): a settlement's payee — the seller who
/// receives the money — must hold complete, jurisdiction-valid tax details, or the concert is not transitioned
/// and not paid (it self-heals on the next sweep once the seller completes onboarding). The payee is
/// direction-dependent: the artist for revenue-share/fixed-fee, the venue for VenueHire. These arrange an
/// incomplete payee by repointing the concert at a seeded operator who never completed organization setup.
/// </summary>
[Collection("Integration")]
public sealed class ConcertPayoutComplianceGateApiTests : IAsyncLifetime
{
    private const decimal DoorRevenue = 200m;

    private readonly ConcertApiFixture fixture;

    public ConcertPayoutComplianceGateApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Guid TenantOf(Guid userId) =>
        fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    private async Task RepointTenantAsync(int concertId, Guid? artistTenantId = null, Guid? venueTenantId = null)
    {
        await fixture.RepointConcertTenantsAsync(concertId, artistTenantId, venueTenantId);
    }

    private Task<ConcertEntity> ConcertAsync(int applicationId) =>
        fixture.Concerts.FirstAsync(value => value.ApplicationId == applicationId);

    // --- Revenue-share (PayoutFinishStep): the artist is the payee ---

    [Fact]
    public async Task Finish_RevenueShare_Defers_WhenPayeeArtistTaxComplianceIncomplete()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await RepointTenantAsync(concert.Id, artistTenantId: TenantOf(fixture.SeedState.ArtistManagerNoArtist.Id));
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        await fixture.FinishConcertAsync(concert.Id);

        Assert.DoesNotContain(fixture.ManagerPaymentClient.Payments, p => p.BookingId == fixture.SeedState.PastDoorSplitBooking.Id);
        var persisted = await ConcertAsync(fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(ConcertState.Draft, persisted.State);
    }

    [Fact]
    public async Task Finish_RevenueShare_Settles_WhenPayeeArtistTaxComplianceComplete()
    {
        // The seeded artist operator completed onboarding, so the gate lets settlement through.
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.PastDoorSplitBooking);
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        await fixture.FinishConcertAsync(concert.Id);

        Assert.Contains(fixture.ManagerPaymentClient.Payments, p => p.BookingId == fixture.SeedState.PastDoorSplitBooking.Id);
        var persisted = await ConcertAsync(fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(ConcertState.AwaitingSettlement, persisted.State);
    }

    // --- Fixed-fee (ReleaseEscrowFinishStep): the artist is the payee ---

    [Fact]
    public async Task Finish_FixedFee_Defers_WhenPayeeArtistTaxComplianceIncomplete()
    {
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking).Id;
        await RepointTenantAsync(concertId, artistTenantId: TenantOf(fixture.SeedState.ArtistManagerNoArtist.Id));

        await fixture.FinishConcertAsync(concertId);

        var persisted = await ConcertAsync(fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(ConcertState.Draft, persisted.State);
    }

    [Fact]
    public async Task Finish_FixedFee_Settles_WhenPayeeArtistTaxComplianceComplete()
    {
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastFlatFeeBooking).Id;

        await fixture.FinishConcertAsync(concertId);

        var persisted = await ConcertAsync(fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(ConcertState.Complete, persisted.State);
    }

    // --- VenueHire direction-flip: the venue is the payee, so it is the tenant gated ---

    [Fact]
    public async Task Finish_VenueHire_Defers_WhenPayeeVenueTaxComplianceIncomplete()
    {
        var concertId = fixture.SeedState.ConcertFor(fixture.SeedState.PastVenueHireBooking).Id;
        await RepointTenantAsync(concertId, venueTenantId: TenantOf(fixture.SeedState.VenueManagerNoVenue.Id));

        await fixture.FinishConcertAsync(concertId);

        var persisted = await ConcertAsync(fixture.SeedState.PastVenueHireApp.Id);
        Assert.Equal(ConcertState.Draft, persisted.State);
    }
}
