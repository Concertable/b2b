using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Phase 4 active-tenant resolution through the real ASP.NET pipeline (resolved by
/// <c>TenantResolutionMiddleware</c>, since <c>/api/organizations</c> is <c>[Authorize]</c>-only). The
/// <c>X-Tenant-Id</c> header names the acting tenant and is validated against membership; a sole membership is
/// the default; a multi-tenant user without a header fails closed. Multi-membership is arranged per test —
/// the seed graph only ever holds one membership per operator.
/// </summary>
[Collection("Integration")]
public sealed class ActiveTenantResolutionTests : IAsyncLifetime
{
    private readonly TenantApiFixture fixture;

    public ActiveTenantResolutionTests(TenantApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    private Guid TenantOf(Guid userId) => fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == userId).Id;

    private HttpClient ClientWithTenant(Guid tenantId)
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());
        return client;
    }

    [Fact]
    public async Task SingleMembership_NoHeader_ResolvesTheSoleTenant()
    {
        var manager = fixture.SeedState.VenueManager1;

        var response = await fixture.CreateClient(manager).GetAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.OK);
        var org = await response.Content.ReadAsync<TenantDetails>();
        Assert.Equal(TenantOf(manager.Id), org!.Id);
    }

    [Fact]
    public async Task MultiMembership_HeaderSwitchesTheActiveTenant()
    {
        var manager = fixture.SeedState.VenueManager1;
        var tenantA = TenantOf(manager.Id);
        var tenantB = TenantOf(fixture.SeedState.VenueManager2.Id);
        await fixture.AddOwnerMembershipAsync(tenantB, manager.Id);

        var orgA = await (await ClientWithTenant(tenantA).GetAsync("/api/organizations")).Content.ReadAsync<TenantDetails>();
        var orgB = await (await ClientWithTenant(tenantB).GetAsync("/api/organizations")).Content.ReadAsync<TenantDetails>();

        Assert.Equal(tenantA, orgA!.Id);
        Assert.Equal(tenantB, orgB!.Id);
    }

    [Fact]
    public async Task MultiMembership_NoHeader_FailsClosed()
    {
        var manager = fixture.SeedState.VenueManager1;
        await fixture.AddOwnerMembershipAsync(TenantOf(fixture.SeedState.VenueManager2.Id), manager.Id);

        // No header + two memberships → no active tenant → the org read sees nothing.
        var response = await fixture.CreateClient(manager).GetAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task HeaderForUnownedTenant_FailsClosed()
    {
        // VenueManager1 has no membership in VenueManager2's tenant — naming it in the header resolves nothing.
        var foreignTenant = TenantOf(fixture.SeedState.VenueManager2.Id);

        var response = await ClientWithTenant(foreignTenant).GetAsync("/api/organizations");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Me_ReturnsCallerMemberships()
    {
        var manager = fixture.SeedState.VenueManager1;

        var response = await fixture.CreateClient(manager).GetAsync("/api/auth/me");

        await response.ShouldBe(HttpStatusCode.OK);
        var me = await response.Content.ReadAsync<MeView>();
        var membership = Assert.Single(me!.Memberships);
        Assert.Equal(TenantOf(manager.Id), membership.TenantId);
        Assert.Equal(TenantType.Venue, membership.Type);
        Assert.Equal(TenantRole.Owner, membership.Role);
    }

    /// <summary>The additive slice of <c>/api/auth/me</c> this phase introduces — the rest of the polymorphic user payload is ignored.</summary>
    private sealed record MeView(IReadOnlyList<MembershipDto> Memberships);
}
