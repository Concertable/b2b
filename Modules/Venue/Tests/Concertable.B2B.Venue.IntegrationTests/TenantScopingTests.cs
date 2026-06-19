using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using static Concertable.B2B.Venue.IntegrationTests.VenueRequestBuilders;
using Xunit.Abstractions;

namespace Concertable.B2B.Venue.IntegrationTests;

[Collection("Integration")]
public sealed class TenantScopingTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public TenantScopingTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    /// <summary>
    /// The write-side guarantee: creating a venue as an operator stamps the row with that operator's tenant —
    /// the <c>TenantInterceptor</c> does it on SaveChanges, the service never sets it. This is the actual
    /// Bucket-A isolation in Phase 2 (reads stay public / UserId-keyed).
    /// </summary>
    [Fact]
    public async Task CreatingVenue_StampsTheCurrentOperatorsTenant()
    {
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var expectedTenantId = fixture.SeedState.Tenants.Single(t => t.CreatedByUserId == manager.Id).Id;

        var client = fixture.CreateClient(manager);
        var response = await client.PostAsync("/api/Venue", await BuildCreateRequest().ToFormContent());
        await response.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadAsync<VenueDetails>();

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IVenueRepository>();
        var venue = await repository.GetByIdAsync(created!.Id);

        Assert.NotNull(venue);
        Assert.Equal(expectedTenantId, venue!.TenantId);
    }

    /// <summary>
    /// <see cref="ITenantScopedRepository{TEntity,TKey}.GetAllByTenantIdAsync"/> returns exactly the rows owned
    /// by the given tenant — the explicit, by-tenant read for admin/reporting.
    /// </summary>
    [Fact]
    public async Task GetAllByTenantId_ReturnsOnlyThatTenantsVenues()
    {
        var grandVenue = fixture.SeedState.Venue;

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IVenueRepository>();

        var ownTenant = await repository.GetAllByTenantIdAsync(grandVenue.TenantId);
        Assert.Contains(ownTenant, v => v.Id == grandVenue.Id);
        Assert.All(ownTenant, v => Assert.Equal(grandVenue.TenantId, v.TenantId));

        var otherTenant = await repository.GetAllByTenantIdAsync(Guid.NewGuid());
        Assert.DoesNotContain(otherTenant, v => v.Id == grandVenue.Id);
    }
}
