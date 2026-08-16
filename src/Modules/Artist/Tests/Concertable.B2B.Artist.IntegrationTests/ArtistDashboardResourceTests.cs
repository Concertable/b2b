using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Artist.IntegrationTests;

[Collection("Integration")]
public sealed class ArtistDashboardResourceTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public ArtistDashboardResourceTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task RecentReviews_ReturnsCurrentArtistReviewsNewestFirst()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<CustomerReviewSubmittedEvent>>()
            .Single(h => h.GetType().Name == "ArtistReviewProjectionHandler");
        await SubmitReviewAsync(handler, "older@example.com", 4, "Older", new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
        await SubmitReviewAsync(handler, "newer@example.com", 5, "Newer", new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/artists/current/reviews/recent");

        await response.ShouldBe(HttpStatusCode.OK);
        var reviews = await response.Content.ReadAsync<List<RecentReviewResponse>>();
        Assert.Equal(2, reviews!.Count);
        var review = reviews[0];
        Assert.Equal("newer@example.com", review.ReviewerName);
        Assert.Equal("Newer", review.Excerpt);
        Assert.Equal($"/_artist/find/artist/{fixture.SeedState.Artist.Id}", review.Href);
    }

    private Task SubmitReviewAsync(
        IIntegrationEventHandler<CustomerReviewSubmittedEvent> handler,
        string email,
        double stars,
        string details,
        DateTimeOffset at)
    {
        var review = new CustomerReviewSubmittedEvent(
            Guid.NewGuid(),
            fixture.SeedState.Artist.Id,
            fixture.SeedState.Venue.Id,
            0,
            stars,
            email,
            details);
        return handler.HandleAsync(review, MessageEnvelope.Create<CustomerReviewSubmittedEvent>(at));
    }

    private sealed record RecentReviewResponse(
        int Id,
        string ReviewerName,
        int Stars,
        string? Excerpt,
        DateTimeOffset At,
        string Href);
}
