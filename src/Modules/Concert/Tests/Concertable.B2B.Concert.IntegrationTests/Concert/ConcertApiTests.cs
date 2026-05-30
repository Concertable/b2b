using System.Net;
using Concertable.Testing.Integration.B2B;
using Xunit;
using static Concertable.B2B.Concert.IntegrationTests.Concert.ConcertRequestBuilders;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

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
            $"/api/Concert/post/{fixture.SeedState.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldReturn403_WhenNotVenueManager()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedState.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenBookingNotConfirmed()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedState.AwaitingPaymentBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ShouldReturn204_WhenPostedSuccessfully()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedState.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenAlreadyPosted()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildPostRequest();

        await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedState.ConfirmedBooking.Concert!.Id}",
            request);

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedState.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion
}