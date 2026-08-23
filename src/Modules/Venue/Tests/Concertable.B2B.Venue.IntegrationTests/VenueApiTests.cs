using System.Net;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Contracts;
using Concertable.B2B.Venue.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.EntityFrameworkCore;
using static Concertable.B2B.Venue.IntegrationTests.VenueRequestBuilders;
using Xunit.Abstractions;

namespace Concertable.B2B.Venue.IntegrationTests;

[Collection("Integration")]

public sealed class VenueApiTests : IAsyncLifetime
{
    private readonly VenueApiFixture fixture;

    public VenueApiTests(VenueApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region GetDetailsById

    [Fact]
    public async Task GetDetailsById_ShouldReturn200_WithVenueDetails()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/venue/{fixture.SeedState.Venue.Id}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var venue = await response.Content.ReadAsync<DetailsResponse>();
        Assert.NotNull(venue);
        Assert.Equal(fixture.SeedState.Venue.Id, venue.Id);
        Assert.Equal("The Grand Venue", venue.Name);
    }

    [Fact]
    public async Task GetDetailsById_ShouldReturn404_WhenVenueDoesNotExist()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/venue/99999");

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetDetails

    [Fact]
    public async Task GetDetails_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/organization/venue");

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDetails_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.GetAsync("/api/organization/venue");

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDetails_ShouldReturn200_WhenVenueExists()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.GetAsync("/api/organization/venue");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var venue = await response.Content.ReadAsync<DetailsResponse>();
        Assert.NotNull(venue);
        Assert.Equal("The Grand Venue", venue.Name);
    }

    [Fact]
    public async Task GetDetails_ShouldReturn204_WhenNoVenueExists()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);

        // Act
        var response = await client.GetAsync("/api/organization/venue");

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();
        var request = BuildCreateRequest();

        // Act
        var response = await client.PostAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var request = BuildCreateRequest();

        // Act
        var response = await client.PostAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WithVenueDto_WhenValidRequest()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);
        var request = BuildCreateRequest();

        // Act
        var response = await client.PostAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Created);
        var venue = await response.Content.ReadAsync<DetailsResponse>();
        Assert.NotNull(venue);
        Assert.True(venue.Id > 0);
        Assert.Equal(request.Name, venue.Name);
        Assert.Equal(request.About, venue.About);
        Assert.Equal(request.Latitude, venue.Latitude);
        Assert.Equal(request.Longitude, venue.Longitude);
        Assert.Equal("Test County", venue.County);
        Assert.Equal("Test Town", venue.Town);
        Assert.Equal("venuemanager35@test.com", venue.Email);
        Assert.False(venue.Approved);
        Assert.EndsWith(".jpg", venue.BannerUrl);
        Assert.True(Guid.TryParse(Path.GetFileNameWithoutExtension(venue.BannerUrl), out _));
        Assert.Equal($"/api/venue/{venue.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenGeocodingFails()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue, o => o.UseFailingGeocoding());
        var request = BuildCreateRequest();

        // Act
        var response = await client.PostAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenNameIsEmpty()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);
        var request = BuildCreateRequest(name: "");

        // Act
        var response = await client.PostAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenProfileAlreadyExists()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildCreateRequest();

        var response = await client.PostAsync(
            "/api/organization/venue",
            await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldEnforceOneProfilePerTenant_WhenRequestsRace()
    {
        var manager = fixture.SeedState.VenueManagerNoVenue;
        var tenantId = fixture.SeedState.Tenants.Single(
            tenant => tenant.CreatedByUserId == manager.Id).Id;
        var client = fixture.CreateClient(manager);

        var responses = await Task.WhenAll(
            client.PostAsync(
                "/api/organization/venue",
                await BuildCreateRequest().ToFormContent()),
            client.PostAsync(
                "/api/organization/venue",
                await BuildCreateRequest().ToFormContent()));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        var profiles = await fixture.Venues
            .Where(value => value.TenantId == tenantId)
            .ToListAsync();
        Assert.Single(profiles);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();
        var request = BuildUpdateRequest();

        // Act
        var response = await client.PutAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var request = BuildUpdateRequest();

        // Act
        var response = await client.PutAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenProfileDoesNotExist()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);
        var request = BuildUpdateRequest();

        var response = await client.PutAsync("/api/organization/venue", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WhenAnotherTenantMemberManagesVenue()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager3);
        client.DefaultRequestHeaders.Add(
            TenantHeaders.TenantId,
            fixture.SeedState.Venue.TenantId.ToString());
        var request = BuildUpdateRequest();

        // Act
        var response = await client.PutAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WithUpdatedVenueDto_WhenValidRequest()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildUpdateRequest();

        // Act
        var response = await client.PutAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var venue = await response.Content.ReadAsync<DetailsResponse>();
        Assert.NotNull(venue);
        Assert.Equal("Updated Venue", venue.Name);
        Assert.Equal("Updated about", venue.About);
        Assert.Equal("Test County", venue.County);
        Assert.Equal("Test Town", venue.Town);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenNameIsEmpty()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildUpdateRequest(name: "");

        // Act
        var response = await client.PutAsync("/api/organization/venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Dashboard

    [Fact]
    public async Task GetDashboardKpis_ShouldReturn200_WhenProfileExists()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/venue-dashboard/kpis");

        await response.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboardKpis_ShouldReturn204_WhenProfileDoesNotExist()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);

        var response = await client.GetAsync("/api/venue-dashboard/kpis");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Approve

    [Fact]
    public async Task Approve_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.PatchAsync($"/api/venue/{fixture.SeedState.Venue.Id}/approve", null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Approve_ShouldReturn403_WhenNotAdmin()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.PatchAsync($"/api/venue/{fixture.SeedState.Venue.Id}/approve", null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approve_ShouldReturn404_WhenVenueDoesNotExist()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.Admin);

        // Act
        var response = await client.PatchAsync("/api/venue/99999/approve", null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Approve_ShouldReturn204_AndApproveVenue()
    {
        // Arrange
        var adminClient = fixture.CreateClient(fixture.SeedState.Admin);
        var client = fixture.CreateClient();

        // Act
        var response = await adminClient.PatchAsync($"/api/venue/{fixture.SeedState.Venue.Id}/approve", null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        var venueResponse = await client.GetAsync($"/api/venue/{fixture.SeedState.Venue.Id}");
        await venueResponse.ShouldBe(HttpStatusCode.OK);
        var venue = await venueResponse.Content.ReadAsync<DetailsResponse>();
        Assert.True(venue!.Approved);
    }

    #endregion

    #region GetPendingApproval

    [Fact]
    public async Task GetPendingApproval_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/venue/pending-approval");

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPendingApproval_ShouldReturn403_WhenNotAdmin()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.GetAsync("/api/venue/pending-approval");

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPendingApproval_ShouldReturn200_WithOnlyUnapprovedVenues()
    {
        // Arrange
        var creator = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);
        var created = await (await creator.PostAsync(
            "/api/organization/venue",
            await BuildCreateRequest().ToFormContent())).Content.ReadAsync<DetailsResponse>();
        var admin = fixture.CreateClient(fixture.SeedState.Admin);

        // Act
        var response = await admin.GetAsync("/api/venue/pending-approval");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadAsync<PendingVenuePage>();
        Assert.NotNull(page);
        Assert.Contains(page.Data, v => v.Id == created!.Id);
        Assert.DoesNotContain(page.Data, v => v.Id == fixture.SeedState.Venue.Id);
    }

    #endregion

    #region IsOwner

    [Fact]
    public async Task IsOwner_ShouldReturnTrue_WhenOwner()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.GetAsync($"/api/venue/{fixture.SeedState.Venue.Id}/ownership");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<bool>();
        Assert.True(result);
    }

    [Fact]
    public async Task IsOwner_ShouldReturnFalse_WhenNotOwner()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        // Act
        var response = await client.GetAsync("/api/venue/99999/ownership");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<bool>();
        Assert.False(result);
    }

    #endregion

    private sealed record PendingVenuePage(List<PendingVenue> Data);
    private sealed record PendingVenue(int Id, string Name, string Email);
}
