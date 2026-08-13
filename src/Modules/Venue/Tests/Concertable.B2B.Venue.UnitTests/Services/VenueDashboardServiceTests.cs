using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.B2B.Venue.UnitTests.Services;

public sealed class VenueDashboardServiceTests
{
    private readonly Mock<IVenueService> venueService = new();
    private readonly Mock<IConcertModule> concertModule = new();
    private readonly Mock<IManagerPaymentReportingClient> reportingClient = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 8, 13, 10, 30, 0, TimeSpan.Zero));
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly VenueDashboardService service;

    public VenueDashboardServiceTests()
    {
        venueService.Setup(s => s.GetIdForCurrentUserAsync()).ReturnsAsync(42);
        tenantContext.SetupGet(t => t.TenantId).Returns(tenantId);
        reportingClient
            .Setup(r => r.GetTicketRevenueAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Money.Gbp(0m));

        service = new VenueDashboardService(
            venueService.Object,
            concertModule.Object,
            reportingClient.Object,
            tenantContext.Object,
            timeProvider);
    }

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

        Assert.NotNull(result);
        Assert.Equal(12345, result!.MtdRevenueCents);
        Assert.Equal(3, result.ApplicationsToReview);
        Assert.Equal(tenantId, capturedPayee);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), capturedPeriod!.Start);
        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime, capturedPeriod.End);
    }

    [Fact]
    public async Task GetKpisAsync_ReturnsNull_WhenCountsUnavailable()
    {
        concertModule
            .Setup(m => m.GetVenueDashboardCountsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VenueDashboardCounts?)null);

        var result = await service.GetKpisAsync();

        Assert.Null(result);
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

        Assert.NotNull(result);
        Assert.Equal(0, result.MtdRevenueCents);
        reportingClient.Verify(
            r => r.GetTicketRevenueAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
