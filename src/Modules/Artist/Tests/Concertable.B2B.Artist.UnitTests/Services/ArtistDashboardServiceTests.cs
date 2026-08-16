using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Artist.Infrastructure.Services;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Client.Enums;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion;

namespace Concertable.B2B.Artist.UnitTests.Services;

public sealed class ArtistDashboardServiceTests
{
    private readonly Mock<IArtistService> artistService = new();
    private readonly Mock<IConcertModule> concertModule = new();
    private readonly Mock<IArtistReviewService> reviewService = new();
    private readonly Mock<IManagerPaymentReportingClient> reportingClient = new();
    private readonly Mock<IPayoutAccountOperationsClient> payoutAccountClient = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly Mock<ITenantModule> tenantModule = new();
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 8, 13, 10, 30, 0, TimeSpan.Zero));
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly ArtistDashboardService service;

    public ArtistDashboardServiceTests()
    {
        artistService.Setup(s => s.GetIdForCurrentTenantAsync()).ReturnsAsync(42);
        tenantContext.SetupGet(t => t.TenantId).Returns(tenantId);
        reviewService.Setup(s => s.GetSummaryAsync(It.IsAny<int>())).ReturnsAsync(new ReviewSummary(0, null));
        payoutAccountClient
            .Setup(c => c.GetAccountStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PayoutAccountStatus.Verified);
        reportingClient
            .Setup(r => r.GetSettlementPayoutsAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Money.Gbp(0m));

        service = new ArtistDashboardService(
            artistService.Object,
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
            new(Guid.NewGuid(), ActivityType.MessageReceived, timeProvider.GetUtcNow(), "New message", null, "/_artist/?inbox=open")
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
        artistService.Setup(s => s.GetDetailsForCurrentUserAsync()).ReturnsAsync(ArtistDetails());
        reviewService.Setup(s => s.GetSummaryAsync(42)).ReturnsAsync(new ReviewSummary(9, 4.5));

        var result = await service.GetOverviewAsync();

        Assert.True(result.TryGetValue(out var overview));
        Assert.Equal(42, overview.ArtistId);
        Assert.Equal(100, overview.ProfileHealth.Completeness);
        Assert.Equal(6, overview.ProfileHealth.Items.Count);
        Assert.Equal(StripeConnectState.Complete, overview.StripeConnect.State);
        Assert.Equal(9, overview.ReviewSummary.TotalReviews);
    }

    #endregion

    #region GetKpisAsync

    [Fact]
    public async Task GetKpisAsync_QueriesTenantMonthToDatePayouts_AndMapsToCents()
    {
        concertModule
            .Setup(m => m.GetArtistDashboardCountsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistDashboardCounts(
                PendingApplications: 4, AcceptedAwaitingCheckout: 2, UpcomingConcerts: 6));

        var capturedPayee = Guid.Empty;
        DateRange? capturedPeriod = null;
        reportingClient
            .Setup(r => r.GetSettlementPayoutsAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DateRange, CancellationToken>((payee, period, _) =>
            {
                capturedPayee = payee;
                capturedPeriod = period;
            })
            .ReturnsAsync(Money.Gbp(67.89m));

        var result = await service.GetKpisAsync();

        Assert.True(result.TryGetValue(out var kpis));
        Assert.Equal(6789, kpis.MtdPayoutsCents);
        Assert.Equal(4, kpis.PendingApplications);
        Assert.Equal(tenantId, capturedPayee);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), capturedPeriod!.Start);
        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime, capturedPeriod.End);
    }

    [Fact]
    public async Task GetKpisAsync_ReturnsNull_WhenCountsUnavailable()
    {
        concertModule
            .Setup(m => m.GetArtistDashboardCountsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.None<ArtistDashboardCounts>());

        var result = await service.GetKpisAsync();

        Assert.False(result.TryGetValue(out _));
    }

    [Fact]
    public async Task GetKpisAsync_AtMonthStart_ReturnsZeroWithoutReportingCall()
    {
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        concertModule
            .Setup(m => m.GetArtistDashboardCountsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArtistDashboardCounts(
                PendingApplications: 4, AcceptedAwaitingCheckout: 2, UpcomingConcerts: 6));

        var result = await service.GetKpisAsync();

        Assert.True(result.TryGetValue(out var kpis));
        Assert.Equal(0, kpis.MtdPayoutsCents);
        reportingClient.Verify(
            r => r.GetSettlementPayoutsAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()),
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
    public async Task GetKpisAsync_WithoutArtist_ReturnsNoneWithoutQueries()
    {
        artistService.Setup(s => s.GetIdForCurrentTenantAsync()).ReturnsAsync(default(Option<int>));

        var result = await service.GetKpisAsync();

        Assert.False(result.TryGetValue(out _));
        concertModule.Verify(
            m => m.GetArtistDashboardCountsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        reportingClient.Verify(
            r => r.GetSettlementPayoutsAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region GetPayoutsAsync

    [Fact]
    public async Task GetPayoutsAsync_SparseSeries_FillsSixCalendarMonths()
    {
        reportingClient
            .Setup(r => r.GetSettlementPayoutsByMonthAsync(tenantId, It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MonthlyPaymentPoint(new DateOnly(2026, 7, 1), Money.Gbp(80m), Money.Gbp(70m), 3)]);

        var result = await service.GetPayoutsAsync();

        Assert.Equal(6, result.Count);
        Assert.Equal(new DateOnly(2026, 3, 1), result[0].Month);
        Assert.Equal(8000, result[4].GrossCents);
        Assert.Equal(7000, result[4].NetCents);
        Assert.Equal(0, result[5].GrossCents);
    }

    #endregion

    private static ArtistDetails ArtistDetails() => new()
    {
        Id = 42,
        Name = "Artist",
        About = "About",
        Genres = [Genre.Rock],
        BannerUrl = "banner",
        Avatar = "avatar",
        County = "West Midlands",
        Town = "Birmingham",
        Email = "artist@example.com"
    };
}
