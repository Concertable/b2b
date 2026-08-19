using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Venue.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion;

namespace Concertable.B2B.Venue.UnitTests.Services;

public sealed class VenueDashboardServiceTests
{
    private readonly Mock<IConcertModule> concertModule = new();
    private readonly Mock<IManagerPaymentReportingClient> reportingClient = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 8, 13, 10, 30, 0, TimeSpan.Zero));
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly VenueDashboardService service;

    public VenueDashboardServiceTests()
    {
        tenantContext.SetupGet(t => t.TenantId).Returns(tenantId);
        reportingClient
            .Setup(r => r.GetTicketRevenueAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Money.Gbp(0m));

        service = new VenueDashboardService(
            concertModule.Object,
            reportingClient.Object,
            tenantContext.Object,
            timeProvider);
    }

    [Fact]
    public async Task GetKpisAsync_QueriesTenantMonthToDateRevenue_AndMapsToCents()
    {
        concertModule
            .Setup(m => m.GetVenueDashboardCountsAsync(tenantId, It.IsAny<CancellationToken>()))
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
    public async Task GetKpisAsync_CountsUnavailable_ReturnsNoneWithoutReportingCall()
    {
        concertModule
            .Setup(m => m.GetVenueDashboardCountsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.None<VenueDashboardCounts>());

        var result = await service.GetKpisAsync();

        Assert.False(result.TryGetValue(out _));
        reportingClient.Verify(
            r => r.GetTicketRevenueAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetKpisAsync_AtMonthStart_ReturnsZeroWithoutReportingCall()
    {
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        concertModule
            .Setup(m => m.GetVenueDashboardCountsAsync(tenantId, It.IsAny<CancellationToken>()))
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
    public async Task GetKpisAsync_AtMonthStartWithoutTenant_ThrowsInvalidOperation()
    {
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        tenantContext.SetupGet(t => t.TenantId).Returns((Guid?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetKpisAsync());
    }
}
