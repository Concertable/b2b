using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Infrastructure.Services;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion;

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
        venueService.Setup(s => s.GetIdForCurrentTenantAsync()).ReturnsAsync(42);
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
}
