using System.Net;
using Concertable.Venue.Application.DTOs;
using Concertable.Venue.Api.Responses;
using static Concertable.Venue.IntegrationTests.VenueRequestBuilders;

namespace Concertable.Venue.IntegrationTests;

[Collection("Integration")]

public class VenueApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public VenueApiTests(ApiFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region GetDetailsById

    [Fact]
    public async Task GetDetailsById_ShouldReturn200_WithVenueDetails()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Venue/{fixture.SeedData.Venue.Id}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var venue = await response.Content.ReadAsync<VenueDetailsResponse>();
        Assert.NotNull(venue);
        Assert.Equal(fixture.SeedData.Venue.Id, venue.Id);
        Assert.Equal("Test Venue", venue.Name);
    }

    [Fact]
    public async Task GetDetailsById_ShouldReturn404_WhenVenueDoesNotExist()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Venue/99999");

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetDetailsForCurrentUser

    [Fact]
    public async Task GetDetailsForCurrentUser_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Venue/user");

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDetailsForCurrentUser_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.ArtistManager1);

        // Act
        var response = await client.GetAsync("/api/Venue/user");

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDetailsForCurrentUser_ShouldReturn200_WhenVenueExists()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        // Act
        var response = await client.GetAsync("/api/Venue/user");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var venue = await response.Content.ReadAsync<VenueDetailsResponse>();
        Assert.NotNull(venue);
        Assert.Equal("Test Venue", venue.Name);
    }

    [Fact]
    public async Task GetDetailsForCurrentUser_ShouldReturn404_WhenNoVenueExists()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager2);

        // Act
        var response = await client.GetAsync("/api/Venue/user");

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
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
        var response = await client.PostAsync("/api/Venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.ArtistManager1);
        var request = BuildCreateRequest();

        // Act
        var response = await client.PostAsync("/api/Venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturn201_WithVenueDto_WhenValidRequest()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager2);
        var request = BuildCreateRequest();

        // Act
        var response = await client.PostAsync("/api/Venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Created);
        var venue = await response.Content.ReadAsync<VenueDto>();
        Assert.NotNull(venue);
        Assert.True(venue.Id > 0);
        Assert.Equal(request.Name, venue.Name);
        Assert.Equal(request.About, venue.About);
        Assert.Equal(request.Latitude, venue.Latitude);
        Assert.Equal(request.Longitude, venue.Longitude);
        Assert.Equal("Test County", venue.County);
        Assert.Equal("Test Town", venue.Town);
        Assert.Equal("venuemanager2@test.com", venue.Email);
        Assert.False(venue.Approved);
        Assert.EndsWith(".jpg", venue.BannerUrl);
        Assert.True(Guid.TryParse(Path.GetFileNameWithoutExtension(venue.BannerUrl), out _));
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenGeocodingFails()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager2, o => o.UseFailingGeocoding());
        var request = BuildCreateRequest();

        // Act
        var response = await client.PostAsync("/api/Venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenNameIsEmpty()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager2);
        var request = BuildCreateRequest(name: "");

        // Act
        var response = await client.PostAsync("/api/Venue", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
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
        var response = await client.PutAsync($"/api/Venue/{fixture.SeedData.Venue.Id}", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.ArtistManager1);
        var request = BuildUpdateRequest();

        // Act
        var response = await client.PutAsync($"/api/Venue/{fixture.SeedData.Venue.Id}", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ShouldReturn403_WhenNotOwner()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager2);
        var request = BuildUpdateRequest();

        // Act
        var response = await client.PutAsync($"/api/Venue/{fixture.SeedData.Venue.Id}", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenVenueDoesNotExist()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        var request = BuildUpdateRequest();

        // Act
        var response = await client.PutAsync("/api/Venue/99999", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ShouldReturn200_WithUpdatedVenueDto_WhenValidRequest()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        var request = BuildUpdateRequest();

        // Act
        var response = await client.PutAsync($"/api/Venue/{fixture.SeedData.Venue.Id}", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var venue = await response.Content.ReadAsync<VenueDto>();
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
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        var request = BuildUpdateRequest(name: "");

        // Act
        var response = await client.PutAsync($"/api/Venue/{fixture.SeedData.Venue.Id}", await request.ToFormContent());

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Approve

    [Fact]
    public async Task Approve_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.PatchAsync($"/api/Venue/{fixture.SeedData.Venue.Id}/approve", null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Approve_ShouldReturn403_WhenNotAdmin()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        // Act
        var response = await client.PatchAsync($"/api/Venue/{fixture.SeedData.Venue.Id}/approve", null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approve_ShouldReturn404_WhenVenueDoesNotExist()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.Admin);

        // Act
        var response = await client.PatchAsync("/api/Venue/99999/approve", null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Approve_ShouldReturn204_AndApproveVenue()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.Admin);

        // Act
        var response = await client.PatchAsync($"/api/Venue/{fixture.SeedData.Venue.Id}/approve", null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        var venue = await fixture.CreateClient().GetAsync<VenueDto>($"/api/Venue/{fixture.SeedData.Venue.Id}");
        Assert.True(venue!.Approved);
    }

    #endregion

    #region IsOwner

    [Fact]
    public async Task IsOwner_ShouldReturnTrue_WhenOwner()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        // Act
        var response = await client.GetAsync($"/api/Venue/{fixture.SeedData.Venue.Id}/ownership");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<bool>();
        Assert.True(result);
    }

    [Fact]
    public async Task IsOwner_ShouldReturnFalse_WhenNotOwner()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);

        // Act
        var response = await client.GetAsync("/api/Venue/99999/ownership");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<bool>();
        Assert.False(result);
    }

    #endregion
}
