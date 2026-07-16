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
/// The fail-closed DAC7 payout gate (<c>FinishExecutor</c>): a settlement's payee — the DAC7 "seller" who
/// receives the money — must hold complete, jurisdiction-valid tax details, or the concert is not transitioned
/// and not paid (it self-heals on the next sweep once the seller completes onboarding). The payee is
/// direction-dependent: the artist for revenue-share/fixed-fee, the venue for VenueHire. These arrange an
/// incomplete payee by repointing the concert at a seeded operator who never completed organization setup.
/// </summary>
[Collection("Integration")]
public sealed class ConcertDac7GateApiTests : IAsyncLifetime
{
    private const decimal DoorRevenue = 200m;

    private readonly ConcertApiFixture fixture;

    public ConcertDac7GateApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
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
        using var scope = fixture.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
        if (artistTenantId is { } artist)
            await context.Concerts.Where(c => c.Id == concertId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ArtistTenantId, artist));
        if (venueTenantId is { } venue)
            await context.Concerts.Where(c => c.Id == concertId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.VenueTenantId, venue));
    }

    private Task<ApplicationEntity> ApplicationAsync(int applicationId) =>
        fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == applicationId);

    // --- Revenue-share (PayoutFinishStep): the artist is the payee ---

    [Fact]
    public async Task Finish_RevenueShare_Defers_WhenPayeeArtistNotDac7Complete()
    {
        var concert = fixture.SeedState.PastDoorSplitBooking.Concert!;
        await RepointTenantAsync(concert.Id, artistTenantId: TenantOf(fixture.SeedState.ArtistManagerNoArtist.Id));
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        await fixture.FinishConcertAsync(concert.Id);

        Assert.DoesNotContain(fixture.ManagerPaymentClient.Payments, p => p.BookingId == fixture.SeedState.PastDoorSplitBooking.Id);
        var application = await ApplicationAsync(fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }

    [Fact]
    public async Task Finish_RevenueShare_Settles_WhenPayeeArtistDac7Complete()
    {
        // The seeded artist operator completed onboarding, so the gate lets settlement through.
        var concert = fixture.SeedState.PastDoorSplitBooking.Concert!;
        await fixture.DeclareDoorRevenueAsync(concert.Id, DoorRevenue);

        await fixture.FinishConcertAsync(concert.Id);

        Assert.Contains(fixture.ManagerPaymentClient.Payments, p => p.BookingId == fixture.SeedState.PastDoorSplitBooking.Id);
        var application = await ApplicationAsync(fixture.SeedState.PastDoorSplitApp.Id);
        Assert.Equal(LifecycleState.AwaitingSettlement, application.State);
    }

    // --- Fixed-fee (ReleaseEscrowFinishStep): the artist is the payee ---

    [Fact]
    public async Task Finish_FixedFee_Defers_WhenPayeeArtistNotDac7Complete()
    {
        var concertId = fixture.SeedState.PastFlatFeeBooking.Concert!.Id;
        await RepointTenantAsync(concertId, artistTenantId: TenantOf(fixture.SeedState.ArtistManagerNoArtist.Id));

        await fixture.FinishConcertAsync(concertId);

        var application = await ApplicationAsync(fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }

    [Fact]
    public async Task Finish_FixedFee_Settles_WhenPayeeArtistDac7Complete()
    {
        var concertId = fixture.SeedState.PastFlatFeeBooking.Concert!.Id;

        await fixture.FinishConcertAsync(concertId);

        var application = await ApplicationAsync(fixture.SeedState.PastFlatFeeApp.Id);
        Assert.Equal(LifecycleState.Complete, application.State);
    }

    // --- VenueHire direction-flip: the venue is the payee, so it is the tenant gated ---

    [Fact]
    public async Task Finish_VenueHire_Defers_WhenPayeeVenueNotDac7Complete()
    {
        var concertId = fixture.SeedState.PastVenueHireBooking.Concert!.Id;
        await RepointTenantAsync(concertId, venueTenantId: TenantOf(fixture.SeedState.VenueManagerNoVenue.Id));

        await fixture.FinishConcertAsync(concertId);

        var application = await ApplicationAsync(fixture.SeedState.PastVenueHireApp.Id);
        Assert.Equal(LifecycleState.Booked, application.State);
    }
}
