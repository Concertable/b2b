using System.Net;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.Contracts.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit.Abstractions;

namespace Concertable.B2B.Dashboard.IntegrationTests;

[Collection("Integration")]
public sealed class OpportunityDashboardApiTests : IAsyncLifetime
{
    private readonly DashboardApiFixture fixture;

    public OpportunityDashboardApiTests(DashboardApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    [Fact]
    public async Task GetCurrentForVenue_MapsApplicationCountAndDeadline()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/opportunity/venue/current");

        await response.ShouldBe(HttpStatusCode.OK);
        var metrics = await response.Content
            .ReadAsync<IReadOnlyList<OpportunityMetricsBoundaryResponse>>();
        Assert.NotNull(metrics);
        var opportunity = fixture.SeedState.Opportunities
            .Where(item => item.VenueId == fixture.SeedState.Venue.Id)
            .Where(item => item.State == OpportunityState.Open)
            .Where(item => item.Period.Start >= fixture.SeedNow)
            .OrderBy(item => item.Period.Start)
            .Take(5)
            .First();
        var metric = Assert.Single(metrics, item => item.Opportunity.Id == opportunity.Id);
        Assert.Equal(
            fixture.SeedState.Applications.Count(item => item.OpportunityId == opportunity.Id),
            metric.ApplicationCount);
        Assert.Equal(
            Math.Max(0, (opportunity.Period.Start.Date.AddDays(-7) - fixture.SeedNow.Date).Days),
            metric.DaysUntilDeadline);
    }

    [Fact]
    public async Task GetRecommendedForArtist_MapsFitAndVenueLocation()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/opportunity/artist/recommended");

        await response.ShouldBe(HttpStatusCode.OK);
        var matches = await response.Content.ReadAsync<IReadOnlyList<OpportunityMatchBoundaryResponse>>();
        Assert.NotNull(matches);
        Assert.NotEmpty(matches);
        Assert.All(matches, match =>
        {
            var venue = fixture.SeedState.Venues.Single(value => value.Id == match.VenueId);
            var expectedFit = match.Genres.Count == 0
                ? 100
                : (int)Math.Round(
                    match.Genres.Count(fixture.SeedState.Artist.Genres.Contains) * 100d /
                    match.Genres.Count);
            Assert.Equal(venue.Address.County, match.County);
            Assert.Equal(venue.Address.Town, match.Town);
            Assert.Equal(expectedFit, match.FitScore);
            Assert.Equal($"/_artist/find/venue/{match.VenueId}", match.Href);
        });
    }

    [Fact]
    public async Task GetRecommendedForArtist_MissingArtist_ReturnsTypedProblem()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync("/api/opportunity/artist/recommended");

        await response.ShouldBe(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
    }

    private sealed record OpportunityMetricsBoundaryResponse(
        OpportunitySummaryBoundaryResponse Opportunity,
        int ApplicationCount,
        int DaysUntilDeadline);

    private sealed record OpportunitySummaryBoundaryResponse(int Id);

    private sealed record OpportunityMatchBoundaryResponse(
        int Id,
        int VenueId,
        string County,
        string Town,
        IReadOnlyList<Genre> Genres,
        int FitScore,
        string Href);
}
