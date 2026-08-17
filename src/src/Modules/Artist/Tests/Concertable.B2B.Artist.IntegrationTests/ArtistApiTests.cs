using System.Net;
using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using static Concertable.B2B.Artist.IntegrationTests.ArtistRequestBuilders;
using Xunit.Abstractions;

namespace Concertable.B2B.Artist.IntegrationTests;

[Collection("Integration")]

public sealed class ArtistApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ArtistApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region GetDetailsById

    [Fact]
    public async Task GetDetailsById_ShouldReturn200_WithArtistDetails()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync($"/api/artist/{fixture.SeedState.Artist.Id}");

        await response.ShouldBe(HttpStatusCode.OK);
        var artist = await response.Content.ReadAsync<DetailsResponse>();
        Assert.NotNull(artist);
        Assert.Equal(fixture.SeedState.Artist.Id, artist.Id);
        Assert.Equal("The Rockers", artist.Name);
    }

    [Fact]
    public async Task GetDetailsById_ShouldReturn404_WhenArtistDoesNotExist()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/artist/99999");

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetDetails

    [Fact]
    public async Task GetDetails_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/organization/artist");

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDetails_ShouldReturn403_WhenNotArtistManager()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/organization/artist");

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDetails_ShouldReturn200_WhenArtistExists()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/organization/artist");

        await response.ShouldBe(HttpStatusCode.OK);
        var artist = await response.Content.ReadAsync<DetailsResponse>();
        Assert.NotNull(artist);
        Assert.Equal("The Rockers", artist.Name);
    }

    [Fact]
    public async Task GetDetails_ShouldReturn204_WhenNoArtistExists()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync("/api/organization/artist");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();
        var request = BuildCreateRequest();

        var response = await client.PostAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn403_WhenNotArtistManager()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildCreateRequest();

        var response = await client.PostAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WithArtistDto_WhenValidRequest()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);
        var request = BuildCreateRequest();

        var response = await client.PostAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.Created);
        var artist = await response.Content.ReadAsync<DetailsResponse>();
        Assert.NotNull(artist);
        Assert.True(artist.Id > 0);
        Assert.Equal(request.Name, artist.Name);
        Assert.Equal(request.About, artist.About);
        Assert.Equal("Test County", artist.County);
        Assert.Equal("Test Town", artist.Town);
        Assert.EndsWith(".jpg", artist.BannerUrl);
        Assert.True(Guid.TryParse(Path.GetFileNameWithoutExtension(artist.BannerUrl), out _));
        Assert.Equal($"/api/artist/{artist.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenGeocodingFails()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist, o => o.UseFailingGeocoding());
        var request = BuildCreateRequest();

        var response = await client.PostAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenNameIsEmpty()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);
        var request = BuildCreateRequest(name: "");

        var response = await client.PostAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenProfileAlreadyExists()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var request = BuildCreateRequest();

        var response = await client.PostAsync(
            "/api/organization/artist",
            await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task LegacyCreateRoute_ShouldReturn404()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.PostAsync(
            "/api/artist",
            await BuildCreateRequest().ToFormContent());

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldEnforceOneProfilePerTenant_WhenRequestsRace()
    {
        var manager = fixture.SeedState.ArtistManagerNoArtist;
        var tenantId = fixture.SeedState.Tenants.Single(
            tenant => tenant.CreatedByUserId == manager.Id).Id;
        var client = fixture.CreateClient(manager);

        var responses = await Task.WhenAll(
            client.PostAsync(
                "/api/organization/artist",
                await BuildCreateRequest().ToFormContent()),
            client.PostAsync(
                "/api/organization/artist",
                await BuildCreateRequest().ToFormContent()));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IArtistRepository>();
        var profiles = await repository.GetAllByTenantIdAsync(tenantId);
        Assert.Single(profiles);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();
        var request = BuildUpdateRequest();

        var response = await client.PutAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ShouldReturn403_WhenNotArtistManager()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildUpdateRequest();

        var response = await client.PutAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenProfileDoesNotExist()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);
        var request = BuildUpdateRequest();

        var response = await client.PutAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LegacyUpdateRoute_ShouldReturn405()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var request = BuildUpdateRequest();

        var response = await client.PutAsync(
            $"/api/artist/{fixture.SeedState.Artist.Id}",
            await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WithUpdatedArtistDto_WhenValidRequest()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var request = BuildUpdateRequest();

        var response = await client.PutAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.OK);
        var artist = await response.Content.ReadAsync<DetailsResponse>();
        Assert.NotNull(artist);
        Assert.Equal("Updated Artist", artist.Name);
        Assert.Equal("Updated about", artist.About);
        Assert.Equal("Test County", artist.County);
        Assert.Equal("Test Town", artist.Town);
    }

    [Fact]
    public async Task Update_ShouldReturn400_WhenNameIsEmpty()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var request = BuildUpdateRequest(name: "");

        var response = await client.PutAsync("/api/organization/artist", await request.ToFormContent());

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Dashboard

    [Fact]
    public async Task GetDashboardKpis_ShouldReturn200_WhenProfileExists()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/artist-dashboard/kpis");

        await response.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboardKpis_ShouldReturn204_WhenProfileDoesNotExist()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync("/api/artist-dashboard/kpis");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LegacyGetRoute_ShouldReturn404()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/artist/user");

        await response.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    #endregion
}
