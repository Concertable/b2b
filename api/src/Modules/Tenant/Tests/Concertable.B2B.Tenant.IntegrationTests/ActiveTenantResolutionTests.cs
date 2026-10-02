using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;
using Xunit.Abstractions;

namespace Concertable.B2B.Tenant.IntegrationTests;

/// <summary>
/// Phase 4 active-tenant resolution through the real ASP.NET pipeline (resolved by
/// <c>TenantResolutionMiddleware</c>, since <c>/api/organization</c> is <c>[Authorize]</c>-only). The
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

        var response = await fixture.CreateClient(manager).GetAsync("/api/organization");

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

        var orgA = await (await ClientWithTenant(tenantA).GetAsync("/api/organization")).Content.ReadAsync<TenantDetails>();
        var orgB = await (await ClientWithTenant(tenantB).GetAsync("/api/organization")).Content.ReadAsync<TenantDetails>();

        Assert.Equal(tenantA, orgA!.Id);
        Assert.Equal(tenantB, orgB!.Id);
    }

    [Fact]
    public async Task MultiMembership_NoHeader_FailsClosed()
    {
        var manager = fixture.SeedState.VenueManager1;
        await fixture.AddOwnerMembershipAsync(TenantOf(fixture.SeedState.VenueManager2.Id), manager.Id);

        // No header + two memberships → no active tenant → the org read sees nothing.
        var response = await fixture.CreateClient(manager).GetAsync("/api/organization");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task HeaderForUnownedTenant_FailsClosed()
    {
        // VenueManager1 has no membership in VenueManager2's tenant — naming it in the header resolves nothing.
        var foreignTenant = TenantOf(fixture.SeedState.VenueManager2.Id);

        var response = await ClientWithTenant(foreignTenant).GetAsync("/api/organization");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MalformedTenantHeader_ReturnsBadRequestProblemDetails()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        client.DefaultRequestHeaders.Add(TenantHeaders.TenantId, "not-a-tenant-id");

        var response = await client.GetAsync("/api/organization");

        await AssertBadTenantHeaderAsync(response);
    }

    [Fact]
    public async Task DuplicateTenantHeaders_ReturnBadRequestProblemDetails()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            TenantHeaders.TenantId,
            [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()]);

        var response = await client.GetAsync("/api/organization");

        await AssertBadTenantHeaderAsync(response);
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
        Assert.Equal(TenantRole.Owner, membership.Role);
        Assert.Equal([TenantBusinessActivityKind.VenueOperator], membership.BusinessActivities);
    }

    private static async Task AssertBadTenantHeaderAsync(HttpResponseMessage response)
    {
        await response.ShouldBe(HttpStatusCode.BadRequest);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Equal("'X-Tenant-Id' is present but is not a tenant id.", problem.Detail);
    }

    /// <summary>The additive slice of <c>/api/auth/me</c> this phase introduces — the rest of the polymorphic user payload is ignored.</summary>
    private sealed record MeView(IReadOnlyList<MembershipDto> Memberships);
}
