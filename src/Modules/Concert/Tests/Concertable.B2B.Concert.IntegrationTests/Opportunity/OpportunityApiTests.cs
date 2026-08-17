using System.Net;
using Concertable.B2B.Concert.Application.DTOs;

using Concertable.B2B.Concert.Api.Responses;
using Xunit;
using static Concertable.B2B.Concert.IntegrationTests.Opportunity.OpportunityRequestBuilders;
using Concertable.B2B.Deal.Contracts;
using Concertable.Contracts;
using Concertable.Contracts.Enums;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Opportunity;

[Collection("Integration")]
public sealed class OpportunityApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public OpportunityApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    public static TheoryData<IDeal> AllDealTypes =>
    [
        new FlatFeeDeal { PaymentMethod = PaymentMethod.Cash, Fee = 500 },
        new DoorSplitDeal { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70 },
        new VersusDeal { PaymentMethod = PaymentMethod.Cash, Guarantee = 200, ArtistDoorPercent = 60 },
        new VenueHireDeal { PaymentMethod = PaymentMethod.Cash, HireFee = 300 },
    ];

    #region Create

    [Theory]
    [MemberData(nameof(AllDealTypes))]
    public async Task Create_ShouldReturnCreatedOpportunity(IDeal deal)
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildRequest(deal, fixture.SeedNow);

        // Act
        var response = await client.PostAsync("/api/opportunity", request);

        // Assert
        await response.ShouldBe(HttpStatusCode.Created);
        var opportunity = await response.Content.ReadAsync<OpportunityDto>();
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
        var request = BuildRequest(new VersusDeal
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
        var request = BuildRequest(new VenueHireDeal
        {
            PaymentMethod = PaymentMethod.Cash,
            HireFee = 0
        }, fixture.SeedNow) with
        {
            Id = fixture.SeedState.FreshVenueHireOpportunity.Id
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
        var result = await response.Content.ReadAsync<Pagination<OpportunityDto>>();
        Assert.NotNull(result);
        Assert.Contains(result.Data, o => o.Id == fixture.SeedState.FreshVenueHireOpportunity.Id);
    }

    #endregion
}
