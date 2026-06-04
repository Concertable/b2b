using System.Net;
using Concertable.B2B.Concert.Application.DTOs;

using Concertable.B2B.Concert.Api.Responses;
using Xunit;
using static Concertable.B2B.Concert.IntegrationTests.Opportunity.OpportunityRequestBuilders;
using Concertable.B2B.Contract.Contracts;
using Concertable.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Opportunity;

[Collection("Integration")]
public sealed class OpportunityApiTests : IAsyncLifetime
{
    private readonly ApiFixture fixture;

    public OpportunityApiTests(ApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    public static TheoryData<IContract> AllContractTypes =>
    [
        new FlatFeeContract { PaymentMethod = PaymentMethod.Cash, Fee = 500 },
        new DoorSplitContract { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70 },
        new VersusContract { PaymentMethod = PaymentMethod.Cash, Guarantee = 200, ArtistDoorPercent = 60 },
        new VenueHireContract { PaymentMethod = PaymentMethod.Cash, HireFee = 300 },
    ];

    #region Create

    [Theory]
    [MemberData(nameof(AllContractTypes))]
    public async Task Create_ShouldReturnCreatedOpportunity(IContract contract)
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var request = BuildRequest(contract);

        // Act
        var response = await client.PostAsync("/api/Opportunity", request);

        // Assert
        var opportunity = await response.Content.ReadAsync<OpportunityDto>();
        Assert.NotNull(opportunity);
        Assert.NotNull(opportunity.Id);
        Assert.Equal(request.StartDate, opportunity.StartDate);
        Assert.Equal(request.EndDate, opportunity.EndDate);
        Assert.Contains(Genre.Rock, opportunity.Genres);
    }

    [Fact]
    public async Task Create_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync("/api/Opportunity", BuildDefaultRequest());

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturn401_WhenUnauthenticated()
    {
        // Arrange
        var client = fixture.CreateClient();

        // Act
        var response = await client.PostAsync("/api/Opportunity", BuildDefaultRequest());

        // Assert
        await response.ShouldBe(HttpStatusCode.Unauthorized);
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
            $"/api/Opportunity/active/venue/{fixture.SeedState.Venue.Id}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadAsync<Pagination<OpportunityDto>>();
        Assert.NotNull(result);
        Assert.Contains(result.Data, o => o.Id == fixture.SeedState.FreshVenueHireOpportunity.Id);
    }

    #endregion
}
