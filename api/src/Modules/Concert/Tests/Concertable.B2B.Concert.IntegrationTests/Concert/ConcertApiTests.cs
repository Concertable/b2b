using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
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
    public async Task GetPublished_ReturnsOnlyPostedConcerts()
    {
        var client = fixture.CreateClient();
        var published = fixture.SeedState.Concerts.First(concert => concert.DatePosted is not null);
        var unpublished = fixture.SeedState.Concerts.First(concert => concert.DatePosted is null);

        await (await client.GetAsync($"/api/concert/{published.Id}"))
            .ShouldBe(HttpStatusCode.OK);
        await (await client.GetAsync($"/api/concert/{unpublished.Id}"))
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUpcomingForManagers_IncludesConcertAlreadyInProgress()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ConcertPrivilegedDbContext>();
        var seededConcert = fixture.SeedState.Concerts.First(concert => concert.DatePosted is not null);
        var concert = await context.Concerts
            .SingleAsync(entity => entity.Id == seededConcert.Id);
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        context.Entry(concert).ComplexProperty(entity => entity.Period).CurrentValue =
            new DateRange(now.AddHours(-1), now.AddHours(1));
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

    private System.Net.Http.HttpClient CreateOwningVenueClient(int venueId) =>
        fixture.CreateClient(fixture.SeedState.VenueManagers.Single(m =>
            m.Id == fixture.SeedState.Venues.Single(v => v.Id == venueId).UserId));

    private System.Net.Http.HttpClient CreateOwningArtistClient(int artistId) =>
        fixture.CreateClient(fixture.SeedState.ArtistManagers.Single(manager =>
            manager.Id == fixture.SeedState.Artists.Single(artist => artist.Id == artistId).UserId));

    #region Update

    [Fact]
    public async Task ConcurrentUpdates_SerializeToCompleteResults()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var client = CreateOwningVenueClient(concert.VenueId);
        var competitor = CreateOwningVenueClient(concert.VenueId);
        var responses = await RaceAsync(
            () => client.PutAsync($"/api/concert/{concert.Id}", BuildPostRequest(name: "First")),
            () => competitor.PutAsync($"/api/concert/{concert.Id}", BuildPostRequest(name: "Second")));

        foreach (var response in responses)
            await response.ShouldBe(HttpStatusCode.OK);
        var persisted = await fixture.Concerts.SingleAsync(value => value.Id == concert.Id);
        Assert.Contains(persisted.Name, new[] { "First", "Second" });
    }

    [Fact]
    public async Task Update_ShouldReturn403_WhenCallerIsNotVenuePrincipal()
    {
        var concert = fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking);
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await client.PutAsync(
            $"/api/concert/{concert.Id}",
            BuildPostRequest(name: "Foreign update"));

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Post

    [Fact]
    public async Task Post_ShouldReturn401_WhenUnauthenticated()
    {
        var client = fixture.CreateClient();
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/concert/post/{fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking).Id}",
            request);

        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_ShouldReturn403_WhenCallerIsNotVenuePrincipal()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/concert/post/{fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking).Id}",
            request);

        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ShouldReturn204_WhenPostedSuccessfully()
    {
        var client = CreateOwningVenueClient(fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking).VenueId);
        var request = BuildPostRequest();

        var response = await client.PutAsync(
            $"/api/concert/post/{fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking).Id}",
            request);

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Post_ShouldReturn400_WhenAlreadyPosted()
    {
        var client = CreateOwningVenueClient(fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking).VenueId);
        var request = BuildPostRequest();

        await client.PutAsync(
            $"/api/concert/post/{fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking).Id}",
            request);

        var response = await client.PutAsync(
            $"/api/concert/post/{fixture.SeedState.ConcertFor(fixture.SeedState.ConfirmedBooking).Id}",
            request);

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("concert.post.invalid", problem.Extensions["code"]?.ToString());
        Assert.Equal(["Concert has already been posted"], problem.Errors["datePosted"]);
    }

    #endregion

    private static async Task<HttpResponseMessage[]> RaceAsync(
        Func<Task<HttpResponseMessage>> first,
        Func<Task<HttpResponseMessage>> second)
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HttpResponseMessage> RunAsync(Func<Task<HttpResponseMessage>> request)
        {
            await start.Task;
            return await request();
        }

        var firstTask = RunAsync(first);
        var secondTask = RunAsync(second);
        start.SetResult();
        return await Task.WhenAll(firstTask, secondTask);
    }
}
