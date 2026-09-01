using System.Net;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.Kernel.DependencyInjection;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Dashboard.IntegrationTests;

[Collection("Integration")]
public sealed class ArtistDashboardApiTests : IAsyncLifetime
{
    private readonly IScoped<IEnumerable<IIntegrationEventHandler<TenantActivityRecordedEvent>>> activityHandlers;
    private readonly DashboardApiFixture fixture;

    public ArtistDashboardApiTests(DashboardApiFixture fixture, ITestOutputHelper output)
    {
        this.fixture = fixture;
        activityHandlers = fixture.Services
            .GetRequiredService<IScoped<IEnumerable<IIntegrationEventHandler<TenantActivityRecordedEvent>>>>();
        fixture.AttachOutput(output);
    }

    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() { fixture.DetachOutput(); return Task.CompletedTask; }

    #region Kpis

    [Fact]
    public async Task GetKpis_NoArtist_ReturnsNoContent()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync("/api/artist-dashboard/kpis");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetKpis_AfterVenueAccepts_CountsAwaitingCheckoutBooking()
    {
        var before = await GetAcceptedAwaitingCheckoutAsync();
        var venueClient = fixture.CreateClient(fixture.SeedState.VenueManager1);
        await venueClient.PostAsync($"/api/application/{fixture.SeedState.FlatFeeApp.Id}/checkout");
        var acceptResponse = await venueClient.PostAsync(
            $"/api/application/{fixture.SeedState.FlatFeeApp.Id}/accept",
            new { eSignature = new { signatoryName = "Test Signatory" } });
        await acceptResponse.ShouldBe(HttpStatusCode.NoContent);

        var after = await GetAcceptedAwaitingCheckoutAsync();

        Assert.Equal(before + 1, after);
    }

    #endregion

    #region Overview

    [Fact]
    public async Task GetOverview_ReturnsCurrentArtistProfileHealth()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/artist-dashboard/overview");

        await response.ShouldBe(HttpStatusCode.OK);
        var overview = await response.Content.ReadAsync<ArtistDashboardOverviewBoundaryResponse>();
        Assert.NotNull(overview);
        Assert.Equal(fixture.SeedState.Artist.Id, overview.ArtistId);
        Assert.Equal(fixture.SeedState.Artist.Name, overview.ArtistName);
        Assert.Contains(overview.ProfileHealth.Items, item => item.Id == "name" && item.Done);
        Assert.Contains(overview.ProfileHealth.Items, item => item.Id == "genres" && item.Done);
    }

    [Fact]
    public async Task GetOverview_NoArtist_ReturnsNoContent()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManagerNoArtist);

        var response = await client.GetAsync("/api/artist-dashboard/overview");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Payouts

    [Fact]
    public async Task GetPayouts_NoPayments_ReturnsSixEmptyMonthlyPoints()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/artist-dashboard/charts/payouts");

        await response.ShouldBe(HttpStatusCode.OK);
        var points = await response.Content.ReadAsync<IReadOnlyList<MonthlyRevenuePointBoundaryResponse>>();
        Assert.NotNull(points);
        Assert.Equal(6, points.Count);
        Assert.Equal(points.OrderBy(point => point.Month), points);
        Assert.All(points, point =>
        {
            Assert.Equal(0, point.GrossCents);
            Assert.Equal(0, point.NetCents);
            Assert.Equal(0, point.Count);
        });
    }

    #endregion

    #region Activity

    [Fact]
    public async Task GetActivity_ReturnsOnlyActiveTenantActivity()
    {
        var at = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        await RecordActivityAsync(
            "test:artist",
            fixture.SeedState.Artist.TenantId,
            "Artist activity",
            at);
        await RecordActivityAsync(
            "test:venue",
            fixture.SeedState.Venue.TenantId,
            "Venue activity",
            at);
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);

        var response = await client.GetAsync("/api/artist-dashboard/activity");

        await response.ShouldBe(HttpStatusCode.OK);
        var activity = await response.Content.ReadAsync<IReadOnlyList<ActivityItemDto>>();
        Assert.NotNull(activity);
        Assert.Contains(activity, item => item.Subject == "Artist activity");
        Assert.DoesNotContain(activity, item => item.Subject == "Venue activity");
    }

    #endregion

    private async Task<int> GetAcceptedAwaitingCheckoutAsync()
    {
        var client = fixture.CreateClient(fixture.SeedState.ArtistManager1);
        var response = await client.GetAsync("/api/artist-dashboard/kpis");
        await response.ShouldBe(HttpStatusCode.OK);
        var counts = await response.Content.ReadAsync<ArtistDashboardBoundaryResponse>();
        Assert.NotNull(counts);
        return counts.AcceptedAwaitingCheckout;
    }

    private Task RecordActivityAsync(string sourceKey, Guid tenantId, string subject, DateTimeOffset at) =>
        activityHandlers.RunAsync(handlers => Task.WhenAll(handlers.Select(handler =>
            handler.HandleAsync(
                new TenantActivityRecordedEvent(new ActivityRecord(
                    sourceKey,
                    tenantId,
                    ActivityType.MessageReceived,
                    at,
                    subject,
                    null,
                    "/?inbox=open")),
                MessageEnvelope.Create<TenantActivityRecordedEvent>(at)))));

    private sealed record ArtistDashboardBoundaryResponse(int AcceptedAwaitingCheckout);

    private sealed record ArtistDashboardOverviewBoundaryResponse(
        int ArtistId,
        string ArtistName,
        ProfileHealthBoundaryResponse ProfileHealth);

    private sealed record ProfileHealthBoundaryResponse(
        int Completeness,
        IReadOnlyList<ProfileHealthItemBoundaryResponse> Items);

    private sealed record ProfileHealthItemBoundaryResponse(string Id, bool Done);

    private sealed record MonthlyRevenuePointBoundaryResponse(
        DateOnly Month,
        long GrossCents,
        long NetCents,
        int Count);
}
