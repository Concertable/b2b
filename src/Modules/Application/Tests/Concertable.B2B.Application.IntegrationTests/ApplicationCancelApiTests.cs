using System.Net;
using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]
public sealed class ApplicationCancelApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationCancelApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region Cancel

    [Fact]
    public async Task Cancel_ShouldMarkCancelledAndNotifyArtist()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel");

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        var application = await fixture.Applications.FirstAsync(a => a.Id == appId);
        Assert.Equal(State.Cancelled, application.State);
        Assert.Contains(await fixture.GetStagedEmailsAsync(), e =>
            e.To == fixture.SeedState.ArtistManager1.Email && e.Subject == "Concert Application Cancelled");
    }

    [Fact]
    public async Task Cancel_ShouldReturn403_WhenCallerIsArtist()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel");

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
        var application = await fixture.Applications.FirstAsync(a => a.Id == appId);
        Assert.Equal(State.Applied, application.State);
    }

    [Fact]
    public async Task Cancel_ShouldReturn404_WhenCallerIsDifferentVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel");

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenAlreadyCancelled()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var firstResponse = await client.PostAsync($"/api/application/{appId}/cancel");
        await firstResponse.ShouldBe(HttpStatusCode.NoContent);

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel");

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_ShouldReturn409_WhenApplicationAlreadyAccepted()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.AwaitingPaymentApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/cancel");

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
        var application = await fixture.Applications.FirstAsync(a => a.Id == appId);
        Assert.Equal(State.Accepted, application.State);
    }

    [Fact]
    public async Task Cancel_ShouldLeaveOpportunityOpenToOtherArtists()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;

        // Act
        var cancelResponse = await client.PostAsync($"/api/application/{appId}/cancel");

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        var opportunitiesResponse = await client.GetAsync($"/api/venue/{fixture.SeedState.Venue.Id}/opportunities");
        await opportunitiesResponse.ShouldBe(HttpStatusCode.OK);
        var opportunities = await opportunitiesResponse.Content.ReadAsync<IEnumerable<OpportunityBoundaryResponse>>();
        Assert.Contains(opportunities!, o => o.Id == opportunityId);
    }

    #endregion

    #region HATEOAS

    [Fact]
    public async Task GetById_ShouldOfferVenueCancel_OnlyWhilePending()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var beforeResponse = await client.GetAsync($"/api/application/{appId}");
        await beforeResponse.ShouldBe(HttpStatusCode.OK);
        var before = await beforeResponse.Content.ReadAsync<ApplicationResponse<VenueApplicationActions>>();
        Assert.Equal(ApplicationStatus.Pending, before!.Status);
        Assert.NotNull(before.Actions.Cancel);
        Assert.Equal($"/api/application/{appId}/cancel", before.Actions.Cancel.Href);

        // Act
        var cancelResponse = await client.PostAsync($"/api/application/{appId}/cancel");

        // Assert
        await cancelResponse.ShouldBe(HttpStatusCode.NoContent);
        var afterResponse = await client.GetAsync($"/api/application/{appId}");
        await afterResponse.ShouldBe(HttpStatusCode.OK);
        var after = await afterResponse.Content.ReadAsync<ApplicationResponse<VenueApplicationActions>>();
        Assert.Equal(ApplicationStatus.Cancelled, after!.Status);
        Assert.Null(after.Actions.Cancel);
        Assert.Null(after.Actions.Accept);
        Assert.Null(after.Actions.Decline);
    }

    [Fact]
    public async Task GetById_ShouldNotOfferVenueCancel_OnceAccepted()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.AwaitingPaymentApp.Id;

        // Act
        var response = await client.GetAsync($"/api/application/{appId}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var application = await response.Content.ReadAsync<ApplicationResponse<VenueApplicationActions>>();
        Assert.Null(application!.Actions.Cancel);
    }

    [Fact]
    public async Task GetById_ShouldNotOfferArtistCancel_WhilePending()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.GetAsync($"/api/application/{appId}");

        // Assert
        await response.ShouldBe(HttpStatusCode.OK);
        var application = await response.Content.ReadAsync<ApplicationResponse<ArtistApplicationActions>>();
        Assert.NotNull(application!.Actions.Withdraw);
    }

    #endregion

    private sealed record OpportunityBoundaryResponse(int Id);
}
