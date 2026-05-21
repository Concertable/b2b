using System.Net;
using Concertable.Testing.Integration;
using Xunit;
using static Concertable.Concert.IntegrationTests.Concert.ConcertRequestBuilders;

namespace Concertable.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public class ConcertApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ConcertApiTests(ApiFixture fixture)
    {
        this.fixture = fixture;
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    #region Post

    [Fact]
    public async Task Post_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedData.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldReturn403_WhenNotVenueManager()
    {
        var client = fixture.CreateClient(fixture.SeedData.ArtistManager1);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedData.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenBookingNotConfirmed()
    {
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedData.AwaitingPaymentBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn204_WhenPostedSuccessfully()
    {
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedData.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenAlreadyPosted()
    {
        var client = fixture.CreateClient(fixture.SeedData.VenueManager1);
        var request = BuildPostRequest();

        await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedData.ConfirmedBooking.Concert!.Id}",
            request);

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedData.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion
}