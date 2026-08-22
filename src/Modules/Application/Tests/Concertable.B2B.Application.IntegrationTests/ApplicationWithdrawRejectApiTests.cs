using System.Net;
using Concertable.B2B.Application.Api.Responses;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Application.IntegrationTests;

[Collection("Integration")]

public sealed class ApplicationWithdrawRejectApiTests : IAsyncLifetime
{
    private readonly ApplicationApiFixture fixture;

    public ApplicationWithdrawRejectApiTests(ApplicationApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region Withdraw

    [Fact]
    public async Task Withdraw_ShouldMarkWithdrawnAndNotifyVenue()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/withdraw");

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        var application = await fixture.Applications.FirstAsync(a => a.Id == appId);
        Assert.Equal(ApplicationState.Withdrawn, application.State);
        Assert.Contains(await fixture.GetStagedEmailsAsync(), e =>
            e.To == fixture.SeedState.VenueManager1.Email && e.Subject == "Concert Application Withdrawn");
    }

    [Fact]
    public async Task Withdraw_ShouldReturn403_WhenCallerIsVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/withdraw");

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
        var application = await fixture.Applications.FirstAsync(a => a.Id == appId);
        Assert.Equal(ApplicationState.Applied, application.State);
    }

    [Fact]
    public async Task Withdraw_ShouldReturn404_WhenCallerIsDifferentArtistTenant()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/withdraw");

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Withdraw_ShouldReturn409_WhenAlreadyWithdrawn()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var firstResponse = await client.PostAsync($"/api/application/{appId}/withdraw");
        await firstResponse.ShouldBe(HttpStatusCode.NoContent);
        var response = await client.PostAsync($"/api/application/{appId}/withdraw");

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Withdraw_ShouldReturn409_WhenAlreadyAccepted()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var applicationId = fixture.SeedState.AwaitingPaymentApp.Id;

        var response = await client.PostAsync(
            $"/api/application/{applicationId}/withdraw",
            (object?)null);

        await response.ShouldBe(HttpStatusCode.Conflict);
        var application = await fixture.Applications.FirstAsync(value => value.Id == applicationId);
        Assert.Equal(ApplicationState.Accepted, application.State);
    }

    [Fact]
    public async Task Withdraw_ShouldLeaveOpportunityOpenToOtherArtists()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;

        // Act
        var withdrawResponse = await client.PostAsync($"/api/application/{appId}/withdraw");

        // Assert
        await withdrawResponse.ShouldBe(HttpStatusCode.NoContent);
        var opportunitiesResponse = await client.GetAsync($"/api/venue/{fixture.SeedState.Venue.Id}/opportunities");
        await opportunitiesResponse.ShouldBe(HttpStatusCode.OK);
        var opportunities = await opportunitiesResponse.Content.ReadAsync<IEnumerable<OpportunityBoundaryResponse>>();
        Assert.Contains(opportunities!, o => o.Id == opportunityId);
    }

    #endregion

    #region Reject

    [Fact]
    public async Task Reject_ShouldMarkRejectedAndNotifyArtist()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/reject");

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        var application = await fixture.Applications.FirstAsync(a => a.Id == appId);
        Assert.Equal(ApplicationState.Rejected, application.State);
        Assert.Contains(await fixture.GetStagedEmailsAsync(), e =>
            e.To == fixture.SeedState.ArtistManager1.Email && e.Subject == "Concert Application Update");
    }

    [Fact]
    public async Task Reject_ShouldReturn403_WhenCallerIsArtist()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/reject");

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
        var application = await fixture.Applications.FirstAsync(a => a.Id == appId);
        Assert.Equal(ApplicationState.Applied, application.State);
    }

    [Fact]
    public async Task Reject_ShouldReturn404_WhenCallerIsDifferentVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/reject");

        // Assert
        await response.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reject_ShouldReturn409_WhenApplicationAlreadyAccepted()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.AwaitingPaymentApp.Id;

        // Act
        var response = await client.PostAsync($"/api/application/{appId}/reject");

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
        var application = await fixture.Applications.FirstAsync(a => a.Id == appId);
        Assert.Equal(ApplicationState.Accepted, application.State);
    }

    #endregion

    #region HATEOAS

    [Fact]
    public async Task GetById_ShouldOfferVenueDecisionLinks_OnlyWhilePending()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var beforeResponse = await client.GetAsync($"/api/application/{appId}");
        await beforeResponse.ShouldBe(HttpStatusCode.OK);
        var before = await beforeResponse.Content.ReadAsync<ApplicationResponse<VenueApplicationActions>>();
        Assert.Equal(ApplicationStatus.Pending, before!.Status);
        Assert.NotNull(before.Actions.Accept);
        Assert.NotNull(before.Actions.Decline);
        Assert.Null(before.Actions.Cancel);

        // Act
        var rejectResponse = await client.PostAsync($"/api/application/{appId}/reject");

        // Assert
        await rejectResponse.ShouldBe(HttpStatusCode.NoContent);
        var afterResponse = await client.GetAsync($"/api/application/{appId}");
        await afterResponse.ShouldBe(HttpStatusCode.OK);
        var after = await afterResponse.Content.ReadAsync<ApplicationResponse<VenueApplicationActions>>();
        Assert.Equal(ApplicationStatus.Rejected, after!.Status);
        Assert.Null(after.Actions.Accept);
        Assert.Null(after.Actions.Decline);
        Assert.Null(after.Actions.Cancel);
    }

    [Fact]
    public async Task GetById_ShouldOfferArtistWithdraw_OnlyWhilePending()
    {
        var artist = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var venue = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        var beforeResponse = await artist.GetAsync($"/api/Application/{appId}");
        await beforeResponse.ShouldBe(HttpStatusCode.OK);
        var before = await beforeResponse.Content.ReadAsync<ApplicationResponse<ArtistApplicationActions>>();
        Assert.NotNull(before!.Actions.Withdraw);

        var rejectResponse = await venue.PostAsync($"/api/Application/{appId}/reject", (object?)null);
        await rejectResponse.ShouldBe(HttpStatusCode.NoContent);

        var afterResponse = await artist.GetAsync($"/api/Application/{appId}");
        await afterResponse.ShouldBe(HttpStatusCode.OK);
        var after = await afterResponse.Content.ReadAsync<ApplicationResponse<ArtistApplicationActions>>();
        Assert.Null(after!.Actions.Withdraw);
    }

    #endregion

    private sealed record OpportunityBoundaryResponse(int Id);
}
