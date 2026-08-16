using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using static Concertable.B2B.Concert.IntegrationTests.Concert.ConcertRequestBuilders;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

[Collection("Integration")]

public sealed class ConcertApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ConcertApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task GetUpcomingForVenue_ShouldReturnConcertList()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/Concert/upcoming/venue/current");

        await response.ShouldBe(HttpStatusCode.OK);
        var concerts = await response.Content.ReadAsync<System.Text.Json.JsonElement>();
        Assert.Equal(System.Text.Json.JsonValueKind.Array, concerts.ValueKind);
    }

    [Fact]
    public async Task GetUpcomingForArtist_ShouldReturnConcertList()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/Concert/upcoming/artist/current");

        await response.ShouldBe(HttpStatusCode.OK);
        var concerts = await response.Content.ReadAsync<System.Text.Json.JsonElement>();
        Assert.Equal(System.Text.Json.JsonValueKind.Array, concerts.ValueKind);
    }

    [Fact]
    public async Task GetUpcomingForManagers_IncludesConcertAlreadyInProgress()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ConcertDbContext>();
        var seededConcert = fixture.SeedState.Concerts.First(concert => concert.DatePosted is not null);
        var concert = await context.Concerts
            .Include(entity => entity.Booking)
            .ThenInclude(booking => booking.Application)
            .ThenInclude(application => application.Opportunity)
            .SingleAsync(entity => entity.Id == seededConcert.Id);
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var opportunity = concert.Booking.Application.Opportunity;
        opportunity.Update(
            new DateRange(now.AddHours(-1), now.AddHours(1)),
            opportunity.DealId,
            opportunity.Genres);
        await context.SaveChangesAsync();

        var venueResponse = await CreateOwningVenueClient(concert.VenueId)
            .GetAsync("/api/Concert/upcoming/venue/current");
        var artistResponse = await CreateOwningArtistClient(concert.ArtistId)
            .GetAsync("/api/Concert/upcoming/artist/current");

        await venueResponse.ShouldBe(HttpStatusCode.OK);
        await artistResponse.ShouldBe(HttpStatusCode.OK);
        var venueConcerts = await venueResponse.Content.ReadAsync<List<ManagerConcertCard>>();
        var artistConcerts = await artistResponse.Content.ReadAsync<List<ManagerConcertCard>>();
        Assert.Contains(venueConcerts!, item => item.Id == concert.Id);
        Assert.Contains(artistConcerts!, item => item.Id == concert.Id);
    }

    /* Posting goes through the booking, which the two-party Tenant filter scopes to its parties —
       so the caller must be the venue manager who actually owns the concert's venue. */
    private System.Net.Http.HttpClient CreateOwningVenueClient(int venueId) =>
        fixture.CreateClient(fixture.SeedState.VenueManagers.Single(m =>
            m.Id == fixture.SeedState.Venues.Single(v => v.Id == venueId).UserId));

    private System.Net.Http.HttpClient CreateOwningArtistClient(int artistId) =>
        fixture.CreateClient(fixture.SeedState.ArtistManagers.Single(manager =>
            manager.Id == fixture.SeedState.Artists.Single(artist => artist.Id == artistId).UserId));

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
        var client = CreateOwningVenueClient(fixture.SeedState.ConfirmedBooking.Concert!.VenueId);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/Concert/post/{fixture.SeedState.ConfirmedBooking.Concert!.Id}",
            request);

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenAlreadyPosted()
    {
        var client = CreateOwningVenueClient(fixture.SeedState.ConfirmedBooking.Concert!.VenueId);
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
