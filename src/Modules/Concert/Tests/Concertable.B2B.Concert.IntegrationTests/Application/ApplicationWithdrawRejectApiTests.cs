using System.Net;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Concertable.B2B.Concert.IntegrationTests.Application;

[Collection("Integration")]

public sealed class ApplicationWithdrawRejectApiTests : IAsyncLifetime
{
    private readonly ConcertApiFixture fixture;

    public ApplicationWithdrawRejectApiTests(ConcertApiFixture fixture, ITestOutputHelper output)
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
        var response = await client.PostAsync($"/api/Application/{appId}/withdraw", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Withdrawn, application.State);
        Assert.Contains(fixture.EmailSender.Sent, e =>
            e.To == fixture.SeedState.VenueManager1.Email && e.Subject == "Concert Application Withdrawn");
    }

    [Fact]
    public async Task Withdraw_ShouldReturn403_WhenCallerIsVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/withdraw", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Applied, application.State);
    }

    [Fact]
    public async Task Withdraw_ShouldReturn404_WhenCallerIsDifferentArtistTenant()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/withdraw", (object?)null);

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
        var firstResponse = await client.PostAsync($"/api/Application/{appId}/withdraw", (object?)null);
        await firstResponse.ShouldBe(HttpStatusCode.NoContent);
        var response = await client.PostAsync($"/api/Application/{appId}/withdraw", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Withdraw_ShouldLeaveOpportunityOpenToOtherArtists()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var opportunityId = fixture.SeedState.FlatFeeApp.OpportunityId;

        // Act
        var withdrawResponse = await client.PostAsync($"/api/Application/{appId}/withdraw", (object?)null);

        // Assert
        await withdrawResponse.ShouldBe(HttpStatusCode.NoContent);
        var opportunitiesResponse = await client.GetAsync($"/api/Venue/{fixture.SeedState.Venue.Id}/opportunities");
        await opportunitiesResponse.ShouldBe(HttpStatusCode.OK);
        var opportunities = await opportunitiesResponse.Content.ReadAsync<IEnumerable<OpportunityResponse>>();
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
        var response = await client.PostAsync($"/api/Application/{appId}/reject", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.NoContent);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Rejected, application.State);
        Assert.Contains(fixture.EmailSender.Sent, e =>
            e.To == fixture.SeedState.Artist.Email && e.Subject == "Concert Application Update");
    }

    [Fact]
    public async Task Reject_ShouldReturn403_WhenCallerIsArtist()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/reject", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Forbidden);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Applied, application.State);
    }

    [Fact]
    public async Task Reject_ShouldReturn404_WhenCallerIsDifferentVenueManager()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager2);
        var appId = fixture.SeedState.FlatFeeApp.Id;

        // Act
        var response = await client.PostAsync($"/api/Application/{appId}/reject", (object?)null);

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
        var response = await client.PostAsync($"/api/Application/{appId}/reject", (object?)null);

        // Assert
        await response.ShouldBe(HttpStatusCode.Conflict);
        var application = await fixture.ConcertReads.Set<ApplicationEntity>().FirstAsync(a => a.Id == appId);
        Assert.Equal(LifecycleState.Accepted, application.State);
    }

    #endregion

    #region HATEOAS

    [Fact]
    public async Task GetById_ShouldOfferWithdrawAndRejectLinks_OnlyWhilePending()
    {
        // Arrange
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);
        var appId = fixture.SeedState.FlatFeeApp.Id;
        var beforeResponse = await client.GetAsync($"/api/Application/{appId}");
        await beforeResponse.ShouldBe(HttpStatusCode.OK);
        var before = await beforeResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Pending, before!.Status);
        Assert.NotNull(before.Actions.Withdraw);
        Assert.NotNull(before.Actions.Reject);

        // Act
        var rejectResponse = await client.PostAsync($"/api/Application/{appId}/reject", (object?)null);

        // Assert
        await rejectResponse.ShouldBe(HttpStatusCode.NoContent);
        var afterResponse = await client.GetAsync($"/api/Application/{appId}");
        await afterResponse.ShouldBe(HttpStatusCode.OK);
        var after = await afterResponse.Content.ReadAsync<ApplicationResponse>();
        Assert.Equal(ApplicationStatus.Rejected, after!.Status);
        Assert.Null(after.Actions.Withdraw);
        Assert.Null(after.Actions.Reject);
    }

    #endregion
}
