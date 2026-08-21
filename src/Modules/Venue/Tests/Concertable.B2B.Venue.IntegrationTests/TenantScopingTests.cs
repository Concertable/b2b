using System.Net;
using Concertable.B2B.Venue.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using static Concertable.B2B.Venue.IntegrationTests.VenueRequestBuilders;
using Xunit.Abstractions;

namespace Concertable.B2B.Venue.IntegrationTests;

[Collection("Integration")]
public sealed class TenantScopingTests : IAsyncLifetime
{
    private readonly VenueApiFixture fixture;

    public TenantScopingTests(VenueApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task CreatingVenue_StampsTheCurrentOperatorsTenant()
    {
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var expectedTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == manager.Id).Id;

        var client = fixture.CreateClient(manager);
        var response = await client.PostAsync(
            "/api/organization/venue",
            await BuildCreateRequest().ToFormContent());
        await response.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadAsync<VenueDetails>();

        var venue = await fixture.Venues.SingleOrDefaultAsync(value => value.Id == created!.Id);

        Assert.NotNull(venue);
        Assert.Equal(expectedTenantId, venue!.TenantId);
    }

    [Fact]
    public async Task GetAllByTenantId_ReturnsOnlyThatTenantsVenues()
    {
        var grandVenue = fixture.SeedState.Venue;

        var ownTenant = await fixture.Venues
            .Where(value => value.TenantId == grandVenue.TenantId)
            .ToListAsync();
        Assert.Contains(ownTenant, v => v.Id == grandVenue.Id);
        Assert.All(ownTenant, v => Assert.Equal(grandVenue.TenantId, v.TenantId));

        var otherTenantId = Guid.NewGuid();
        var otherTenant = await fixture.Venues
            .Where(value => value.TenantId == otherTenantId)
            .ToListAsync();
        Assert.DoesNotContain(otherTenant, v => v.Id == grandVenue.Id);
    }
}
