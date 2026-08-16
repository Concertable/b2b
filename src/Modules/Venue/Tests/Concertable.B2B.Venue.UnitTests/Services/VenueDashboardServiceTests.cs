using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Infrastructure.Services;
using Concertable.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Client.Enums;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion;

namespace Concertable.B2B.Venue.UnitTests.Services;

public sealed class VenueDashboardServiceTests
{
    private readonly Mock<IVenueService> venueService = new();
    private readonly Mock<IConcertModule> concertModule = new();
    private readonly Mock<IVenueReviewService> reviewService = new();
    private readonly Mock<IManagerPaymentReportingClient> reportingClient = new();
    private readonly Mock<IPayoutAccountOperationsClient> payoutAccountClient = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly Mock<ITenantModule> tenantModule = new();
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 8, 13, 10, 30, 0, TimeSpan.Zero));
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly VenueDashboardService service;

    public VenueDashboardServiceTests()
    {
        venueService.Setup(s => s.GetIdForCurrentTenantAsync()).ReturnsAsync(42);
        tenantContext.SetupGet(t => t.TenantId).Returns(tenantId);
        reviewService.Setup(s => s.GetSummaryAsync(It.IsAny<int>())).ReturnsAsync(new ReviewSummary(0, null));
        payoutAccountClient
            .Setup(c => c.GetAccountStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PayoutAccountStatus.Verified);
        reportingClient
            .Setup(r => r.GetTicketRevenueAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Money.Gbp(0m));

        service = new VenueDashboardService(
            venueService.Object,
            concertModule.Object,
            reviewService.Object,
            reportingClient.Object,
            payoutAccountClient.Object,
            tenantContext.Object,
            tenantModule.Object,
            timeProvider);
    }

    [Fact]
    public async Task GetActivityAsync_UsesActiveTenantAndDashboardLimit()
    {
        ActivityItemDto[] expected =
        [
            new(Guid.NewGuid(), ActivityType.MessageReceived, timeProvider.GetUtcNow(), "New message", null, "/_venue/?inbox=open")
        ];
        tenantModule
            .Setup(m => m.GetRecentActivityAsync(tenantId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await service.GetActivityAsync();

        Assert.Same(expected, result);
    }

    #region GetOverviewAsync

    [Fact]
    public async Task GetOverviewAsync_CompleteProfile_CombinesProfilePaymentAndReview()
    {
        venueService.Setup(s => s.GetDetailsForCurrentUserAsync()).ReturnsAsync(VenueDetails());
        reviewService.Setup(s => s.GetSummaryAsync(42)).ReturnsAsync(new ReviewSummary(12, 4.75));

        var result = await service.GetOverviewAsync();

        Assert.True(result.TryGetValue(out var overview));
        Assert.Equal(42, overview.VenueId);
        Assert.Equal(100, overview.ProfileHealth.Completeness);
        Assert.Equal(5, overview.ProfileHealth.Items.Count);
        Assert.Equal(StripeConnectState.Complete, overview.StripeConnect.State);
        Assert.Equal(12, overview.ReviewSummary.TotalReviews);
    }

    #endregion

    #region GetKpisAsync

    [Fact]
    public async Task GetKpisAsync_QueriesTenantMonthToDateRevenue_AndMapsToCents()
    {
        concertModule
            .Setup(m => m.GetVenueDashboardCountsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VenueDashboardCounts(
                ApplicationsToReview: 3, OpenOpportunities: 2, UpcomingConcerts: 5, AwaitingDoorRevenue: 1));

        var capturedPayee = Guid.Empty;
        DateRange? capturedPeriod = null;
        reportingClient
            .Setup(r => r.GetTicketRevenueAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DateRange, CancellationToken>((payee, period, _) =>
            {
                capturedPayee = payee;
                capturedPeriod = period;
            })
            .ReturnsAsync(Money.Gbp(123.45m));

        var result = await service.GetKpisAsync();

        Assert.True(result.TryGetValue(out var kpis));
        Assert.Equal(12345, kpis.MtdRevenueCents);
        Assert.Equal(3, kpis.ApplicationsToReview);
        Assert.Equal(tenantId, capturedPayee);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), capturedPeriod!.Start);
        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime, capturedPeriod.End);
    }

    [Fact]
    public async Task GetKpisAsync_ReturnsNull_WhenCountsUnavailable()
    {
        concertModule
            .Setup(m => m.GetVenueDashboardCountsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.None<VenueDashboardCounts>());

        var result = await service.GetKpisAsync();

        Assert.False(result.TryGetValue(out _));
    }

    [Fact]
    public async Task GetKpisAsync_AtMonthStart_ReturnsZeroWithoutReportingCall()
    {
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        concertModule
            .Setup(m => m.GetVenueDashboardCountsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VenueDashboardCounts(
                ApplicationsToReview: 3, OpenOpportunities: 2, UpcomingConcerts: 5, AwaitingDoorRevenue: 1));

        var result = await service.GetKpisAsync();

        Assert.True(result.TryGetValue(out var kpis));
        Assert.Equal(0, kpis.MtdRevenueCents);
        reportingClient.Verify(
            r => r.GetTicketRevenueAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetKpisAsync_AtMonthStartWithoutTenant_ThrowsForbidden()
    {
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        tenantContext.SetupGet(t => t.TenantId).Returns((Guid?)null);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetKpisAsync());
    }

    [Fact]
    public async Task GetKpisAsync_WithoutVenue_ReturnsNoneWithoutQueries()
    {
        venueService.Setup(s => s.GetIdForCurrentTenantAsync()).ReturnsAsync(default(Option<int>));

        var result = await service.GetKpisAsync();

        Assert.False(result.TryGetValue(out _));
        concertModule.Verify(
            m => m.GetVenueDashboardCountsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        reportingClient.Verify(
            r => r.GetTicketRevenueAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetTicketRevenueAsync

    [Fact]
    public async Task GetTicketRevenueAsync_SparseSeries_FillsSixCalendarMonths()
    {
        reportingClient
            .Setup(r => r.GetTicketRevenueByMonthAsync(tenantId, It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MonthlyPaymentPoint(new DateOnly(2026, 6, 1), Money.Gbp(50m), Money.Gbp(40m), 2)]);

        var result = await service.GetTicketRevenueAsync();

        Assert.Equal(6, result.Count);
        Assert.Equal(new DateOnly(2026, 3, 1), result[0].Month);
        Assert.Equal(5000, result[3].GrossCents);
        Assert.Equal(4000, result[3].NetCents);
        Assert.Equal(0, result[5].GrossCents);
    }

    #endregion

    #region GetSettlementsAsync

    [Fact]
    public async Task GetSettlementsAsync_PaymentReports_EnrichesConcertAndDirection()
    {
        var artistTenantId = Guid.NewGuid();
        reportingClient
            .Setup(r => r.GetRecentSettlementsAsync(tenantId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ManagerSettlement(8, 12, tenantId, artistTenantId, Money.Gbp(75m), new DateTime(2026, 8, 2)),
                new ManagerSettlement(9, 13, artistTenantId, tenantId, Money.Gbp(25m), new DateTime(2026, 8, 3))
            ]);
        concertModule
            .Setup(m => m.GetManagerSettlementContextsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ManagerSettlementContext(12, 112, "First", tenantId, artistTenantId, "Venue", "Artist"),
                new ManagerSettlementContext(13, 113, "Second", tenantId, artistTenantId, "Venue", "Artist")
            ]);

        var result = await service.GetSettlementsAsync();

        Assert.Equal(SettlementDirection.Out, result[0].Direction);
        Assert.Equal(SettlementDirection.In, result[1].Direction);
        Assert.All(result, settlement => Assert.Equal("Artist", settlement.CounterpartyName));
        Assert.Equal(112, result[0].ConcertId);
    }

    #endregion

    private static VenueDetails VenueDetails() => new()
    {
        Id = 42,
        Name = "Venue",
        About = "About",
        BannerUrl = "banner",
        Avatar = "avatar",
        Latitude = 52.48,
        Longitude = -1.90,
        County = "West Midlands",
        Town = "Birmingham",
        Email = "venue@example.com"
    };
}
