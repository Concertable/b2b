using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Venue.Domain.ReadModels;
using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Venue.IntegrationTests;

[Collection("Integration")]
public sealed class VenueDashboardResourceTests : IAsyncLifetime
{
    private readonly VenueApiFixture fixture;

    public VenueDashboardResourceTests(VenueApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task RecentReviews_ReturnsCurrentVenueReviewsNewestFirst()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<VenueDbContext>();
        context.VenueReviews.AddRange(
            new VenueReview
            {
                VenueId = fixture.SeedState.Venue.Id,
                Email = "older@example.com",
                Stars = 4,
                Details = "Older",
                CreatedAt = new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero)
            },
            new VenueReview
            {
                VenueId = fixture.SeedState.Venue.Id,
                Email = "newer@example.com",
                Stars = 5,
                Details = "Newer",
                CreatedAt = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero)
            });
        await context.SaveChangesAsync();
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/organization/venue/review/recent");

        await response.ShouldBe(HttpStatusCode.OK);
        var reviews = await response.Content.ReadAsync<List<RecentReviewResponse>>();
        Assert.Equal(2, reviews!.Count);
        var review = reviews[0];
        Assert.Equal("newer@example.com", review.ReviewerName);
        Assert.Equal("Newer", review.Excerpt);
        Assert.Equal($"/_venue/find/venue/{fixture.SeedState.Venue.Id}", review.Href);
    }

    private sealed record RecentReviewResponse(
        int Id,
        string ReviewerName,
        int Stars,
        string? Excerpt,
        DateTimeOffset At,
        string Href);
}
