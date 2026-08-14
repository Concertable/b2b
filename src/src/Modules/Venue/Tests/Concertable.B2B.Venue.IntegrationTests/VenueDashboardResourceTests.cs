using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Domain.ReadModels;
using Concertable.B2B.Venue.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Messaging.Contracts;
using Xunit.Abstractions;

namespace Concertable.B2B.Venue.IntegrationTests;

[Collection("Integration")]
public sealed class VenueDashboardResourceTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public VenueDashboardResourceTests(ApiFixture fixture, ITestOutputHelper output)
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

        var response = await client.GetAsync("/api/venues/current/reviews/recent?take=1");

        await response.ShouldBe(HttpStatusCode.OK);
        var reviews = await response.Content.ReadAsync<List<RecentReviewDto>>();
        var review = Assert.Single(reviews!);
        Assert.Equal("newer@example.com", review.ReviewerName);
        Assert.Equal("Newer", review.Excerpt);
    }

    [Fact]
    public async Task Activity_ReturnsOnlyTheActiveTenantActivity()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<TenantActivityRecordedEvent>>();
        var at = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        await handler.HandleAsync(new TenantActivityRecordedEvent(new ActivityRecord(
            "test:venue",
            fixture.SeedState.Venue.TenantId,
            ActivityType.MessageReceived,
            at,
            "Venue activity",
            null,
            "/_venue/?inbox=open")), MessageEnvelope.Create<TenantActivityRecordedEvent>(at));
        await handler.HandleAsync(new TenantActivityRecordedEvent(new ActivityRecord(
            "test:artist",
            fixture.SeedState.Artist.TenantId,
            ActivityType.MessageReceived,
            at,
            "Artist activity",
            null,
            "/_artist/?inbox=open")), MessageEnvelope.Create<TenantActivityRecordedEvent>(at));
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/VenueDashboard/activity?take=10");

        await response.ShouldBe(HttpStatusCode.OK);
        var activity = await response.Content.ReadAsync<List<ActivityItemDto>>();
        var item = Assert.Single(activity!);
        Assert.Equal("Venue activity", item.Subject);
    }
}
