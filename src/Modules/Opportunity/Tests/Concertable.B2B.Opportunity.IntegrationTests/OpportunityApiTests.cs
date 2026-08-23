using System.Net;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Opportunity.Api.Responses;
using Concertable.B2B.Opportunity.Application.Requests;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.Contracts;
using Concertable.Contracts.Enums;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
using static Concertable.B2B.Opportunity.IntegrationTests.OpportunityRequestBuilders;

namespace Concertable.B2B.Opportunity.IntegrationTests;

[Collection("Integration")]
public sealed class OpportunityApiTests : IAsyncLifetime
{
    private readonly OpportunityApiFixture fixture;

    public OpportunityApiTests(OpportunityApiFixture fixture, ITestOutputHelper output)
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

        var response = await client.GetAsync("/api/Opportunity/venue/current");

        await response.ShouldBe(HttpStatusCode.OK);
        var metrics = await response.Content.ReadAsync<IReadOnlyList<OpportunityApplicationMetricsResponse>>();
        Assert.NotNull(metrics);
        var opportunity = fixture.SeedState.ActiveVenueHireOpportunity;
        var metric = Assert.Single(metrics, item => item.Opportunity.Id == opportunity.Id);
        Assert.Equal(1, metric.ApplicationCount);
        Assert.Equal(
            Math.Max(0, (opportunity.Period.Start.Date.AddDays(-7) - fixture.SeedNow.Date).Days),
            metric.DaysUntilDeadline);
    }

    [Fact]
    public async Task GetRecommendedForArtist_MapsFitAndVenueLocation()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/Opportunity/artist/recommended");

        await response.ShouldBe(HttpStatusCode.OK);
        var matches = await response.Content.ReadAsync<IReadOnlyList<OpportunityMatchResponse>>();
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
            Assert.Equal(venue.County, match.County);
            Assert.Equal(venue.Town, match.Town);
            Assert.Equal(expectedFit, match.FitScore);
            Assert.Equal($"/api/application/opportunity/{match.Id}/checkout", match.Href);
        });
    }

    [Fact]
    public async Task GetRecommendedForArtist_MissingArtist_ReturnsTypedProblem()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync("/api/Opportunity/artist/recommended");

        await response.ShouldBe(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("opportunity.query.missing_artist", problem.Extensions["code"]?.ToString());
    }

    public static TheoryData<DealDto> AllDealTypes =>
    [
        new FlatFeeDealDto { PaymentMethod = PaymentMethod.Cash, Fee = 500 },
        new DoorSplitDealDto { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70 },
        new VersusDealDto { PaymentMethod = PaymentMethod.Cash, Guarantee = 200, ArtistDoorPercent = 60 },
        new VenueHireDealDto { PaymentMethod = PaymentMethod.Cash, HireFee = 300 },
    ];

    #region Create

    [Theory]
    [MemberData(nameof(AllDealTypes))]
    public async Task Create_ShouldReturnCreatedOpportunity(DealDto deal)
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildRequest(deal, fixture.SeedNow);

        // Act
        var response = await client.PostAsync("/api/opportunity", request);

        // Assert
        await response.ShouldBe(HttpStatusCode.Created);
        var opportunity = await response.Content.ReadAsync<OpportunityResponse>();
        Assert.NotNull(opportunity);
        Assert.NotNull(opportunity.Id);
        Assert.Equal(request.StartDate, opportunity.StartDate);
        Assert.Equal(request.EndDate, opportunity.EndDate);
        Assert.Contains(Genre.Rock, opportunity.Genres);
        Assert.Equal($"/api/opportunity/{opportunity.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync("/api/opportunity", BuildDefaultRequest(fixture.SeedNow));

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/api/opportunity", BuildDefaultRequest(fixture.SeedNow));

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_InvalidDeal_ReturnsValidationProblem()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildRequest(new VersusDealDto
        {
            PaymentMethod = PaymentMethod.Cash,
            Guarantee = -1,
            ArtistDoorPercent = 101
        }, fixture.SeedNow);

        var response = await client.PostAsync("/api/opportunity", request);

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Extensions.TryGetValue("code", out var code));
        Assert.Equal("opportunity.deal.invalid", code?.ToString());
        Assert.Equal(["Guarantee must be zero or greater."], problem.Errors["Guarantee"]);
        Assert.Equal(
            ["Artist door percent must be between 0 and 100."],
            problem.Errors["ArtistDoorPercent"]);
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_InvalidDeal_ReturnsValidationProblem()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildRequest(new VenueHireDealDto
        {
            PaymentMethod = PaymentMethod.Cash,
            HireFee = 0
        }, fixture.SeedNow) with
        {
            Id = fixture.SeedState.ActiveVenueHireOpportunity.Id
        };

        var response = await client.PutAsync(
            $"/api/venue/{fixture.SeedState.Venue.Id}/opportunities",
            new[] { request });

        await response.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Extensions.TryGetValue("code", out var code));
        Assert.Equal("opportunity.deal.invalid", code?.ToString());
        Assert.Equal(["Hire fee must be greater than zero."], problem.Errors["HireFee"]);
    }

    [Fact]
    public async Task Update_OmittedOpportunity_IsWithdrawnInsteadOfDeleted()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var opportunityId = fixture.SeedState.ActiveVenueHireOpportunity.Id;

        var response = await client.PutAsync(
            $"/api/venue/{fixture.SeedState.Venue.Id}/opportunities",
            Array.Empty<OpportunityRequest>());

        await response.ShouldBe(HttpStatusCode.OK);
        var opportunity = await fixture.Opportunities.SingleAsync(value => value.Id == opportunityId);
        Assert.Equal(OpportunityState.Withdrawn, opportunity.State);
    }

    #endregion

    #region GetActiveByVenueId

    [Fact]
    public async Task GetActiveByVenueId_ShouldReturnSeededOpportunity()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.GetAsync(
            $"/api/opportunity/active/venue/{fixture.SeedState.Venue.Id}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<Pagination<OpportunityResponse>>();
        Assert.NotNull(result);
        Assert.Contains(result.Data, o => o.Id == fixture.SeedState.ActiveVenueHireOpportunity.Id);
    }

    #endregion
}
