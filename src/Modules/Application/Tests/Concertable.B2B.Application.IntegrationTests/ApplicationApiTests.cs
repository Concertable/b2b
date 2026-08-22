using System.Net;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]

public sealed class ApplicationApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region Eligibility

    [Fact]
    public async Task CanApply_EligibleArtist_ReturnsTrue()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync(
            $"/api/application/opportunity/{fixture.SeedState.FreshVenueHireOpportunity.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.True(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanApply_MissingArtist_ReturnsFalse()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync(
            $"/api/application/opportunity/{fixture.SeedState.FreshVenueHireOpportunity.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.False(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanAccept_EligibleApplication_ReturnsTrue()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync(
            $"/api/application/{fixture.SeedState.FlatFeeApp.Id}/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.True(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task CanAccept_MissingApplication_ReturnsFalse()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/application/2147483647/eligibility");

        await response.ShouldBe(HttpStatusCode.OK);
        Assert.False(await response.Content.ReadAsync<bool>());
    }

    [Fact]
    public async Task GetByOpportunity_ForeignVenue_ReturnsForbiddenProblem()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        var response = await client.GetAsync(
            $"/api/application/opportunity/{fixture.SeedState.FlatFeeApp.OpportunityId}");

        await AssertProblemCodeAsync(
            response,
            HttpStatusCode.Forbidden,
            "application.query.opportunity_forbidden");
    }

    [Theory]
    [InlineData("/api/application/artist/pending")]
    [InlineData("/api/application/artist/recently-denied")]
    public async Task ArtistQueries_MissingArtist_ReturnForbiddenProblem(string path)
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync(path);

        await AssertProblemCodeAsync(response, HttpStatusCode.Forbidden, "application.query.missing_artist");
    }

    [Fact]
    public async Task ApplyCheckout_MissingArtist_ReturnsForbiddenProblem()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.PostAsync(
            $"/api/application/opportunity/{fixture.SeedState.FreshVenueHireOpportunity.Id}/checkout",
            null);

        await AssertProblemCodeAsync(response, HttpStatusCode.Forbidden, "application.eligibility.missing_artist");
    }

    #endregion

    #region Accept

    [Fact]
    public async Task Accept_ShouldReturn403_WhenNotVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        // Act
        var response = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Accept_ShouldReturn404_WhenCalledByDifferentVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);

        // Act
        var response = await client.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept", new { eSignature = new { signatoryName = "Test Signatory" } });

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Apply

    [Fact]
    public async Task Apply_ShouldReturn400_WhenSameArtistReappliesAfterWithdraw()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;
        var withdraw = await client.PostAsync($"/api/application/{appId}/withdraw");
        await withdraw.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var response = await client.PostAsync($"/api/application/{opportunityId}", new { eSignature = new { signatoryName = "Aretha Artist" } });

        // Assert
        await response.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    private static async Task AssertProblemCodeAsync(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string expectedCode)
    {
        await response.ShouldBe(statusCode);
        var problem = await response.Content.ReadAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Extensions.TryGetValue("code", out var code));
        Assert.Equal(expectedCode, code?.ToString());
    }
}
