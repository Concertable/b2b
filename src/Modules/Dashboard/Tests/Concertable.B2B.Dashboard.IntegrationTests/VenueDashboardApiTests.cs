using System.Net;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.Kernel.DependencyInjection;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Concertable.B2B.Dashboard.IntegrationTests;

[Collection("Integration")]
public sealed class VenueDashboardApiTests : IAsyncLifetime
{
    private readonly IScoped<IEnumerable<IIntegrationEventHandler<TenantActivityRecordedEvent>>> activityHandlers;
    private readonly DashboardApiFixture fixture;

    public VenueDashboardApiTests(DashboardApiFixture fixture, ITestOutputHelper output)
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
    public async Task GetKpis_ReturnsCurrentVenueMetrics()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/venue-dashboard/kpis");

        await response.ShouldBe(HttpStatusCode.OK);
        var kpis = await response.Content.ReadAsync<VenueDashboardKpisBoundaryResponse>();
        Assert.NotNull(kpis);
        Assert.True(kpis.OpenOpportunities > 0);
        Assert.True(kpis.UpcomingConcerts > 0);
        Assert.Equal(0, kpis.MtdRevenueCents);
    }

    [Fact]
    public async Task GetKpis_NoVenue_ReturnsNoContent()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);

        var response = await client.GetAsync("/api/venue-dashboard/kpis");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Overview

    [Fact]
    public async Task GetOverview_ReturnsCurrentVenueProfileHealth()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/venue-dashboard/overview");

        await response.ShouldBe(HttpStatusCode.OK);
        var overview = await response.Content.ReadAsync<VenueDashboardOverviewBoundaryResponse>();
        Assert.NotNull(overview);
        Assert.Equal(fixture.SeedState.Venue.Id, overview.VenueId);
        Assert.Equal(fixture.SeedState.Venue.Name, overview.VenueName);
        Assert.Contains(overview.ProfileHealth.Items, item => item.Id == "name" && item.Done);
        Assert.Contains(overview.ProfileHealth.Items, item => item.Id == "bio" && item.Done);
    }

    [Fact]
    public async Task GetOverview_NoVenue_ReturnsNoContent()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManagerNoVenue);

        var response = await client.GetAsync("/api/venue-dashboard/overview");

        await response.ShouldBe(HttpStatusCode.NoContent);
    }

    #endregion

    #region Ticket revenue

    [Fact]
    public async Task GetPaymentRevenue_NoPayments_ReturnsSixEmptyMonthlyPoints()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/venue-dashboard/charts/payment-revenue");

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

    #region Settlements

    [Fact]
    public async Task GetSettlements_NoPayments_ReturnsEmptyCollection()
    {
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/venue-dashboard/settlements");

        await response.ShouldBe(HttpStatusCode.OK);
        var settlements = await response.Content.ReadAsync<IReadOnlyList<SettlementBoundaryResponse>>();
        Assert.NotNull(settlements);
        Assert.Empty(settlements);
    }

    #endregion

    #region Activity

    [Fact]
    public async Task GetActivity_ReturnsOnlyActiveTenantActivity()
    {
        var at = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
        await RecordActivityAsync(
            "test:venue",
            fixture.SeedState.Venue.TenantId,
            "Venue activity",
            at);
        await RecordActivityAsync(
            "test:artist",
            fixture.SeedState.Artist.TenantId,
            "Artist activity",
            at);
        var client = fixture.CreateClient(fixture.SeedState.VenueManager1);

        var response = await client.GetAsync("/api/venue-dashboard/activity");

        await response.ShouldBe(HttpStatusCode.OK);
        var activity = await response.Content.ReadAsync<IReadOnlyList<ActivityItemDto>>();
        Assert.NotNull(activity);
        Assert.Contains(activity, item => item.Subject == "Venue activity");
        Assert.DoesNotContain(activity, item => item.Subject == "Artist activity");
    }

    #endregion

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

    private sealed record VenueDashboardKpisBoundaryResponse(
        int OpenOpportunities,
        int UpcomingConcerts,
        long MtdRevenueCents);

    private sealed record VenueDashboardOverviewBoundaryResponse(
        int VenueId,
        string VenueName,
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

    private sealed record SettlementBoundaryResponse(int Id);
}
