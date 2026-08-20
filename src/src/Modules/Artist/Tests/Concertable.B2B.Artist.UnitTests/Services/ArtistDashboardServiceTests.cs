using Concertable.B2B.Artist.Infrastructure.Services;
using Concertable.B2B.Concert.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion;

namespace Concertable.B2B.Artist.UnitTests.Services;

public sealed class ArtistDashboardServiceTests
{
    private readonly Mock<IConcertModule> concertModule = new();
    private readonly Mock<IManagerPaymentReportingClient> reportingClient = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 8, 13, 10, 30, 0, TimeSpan.Zero));
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly ArtistDashboardService service;

    public ArtistDashboardServiceTests()
    {
        tenantContext.SetupGet(t => t.TenantId).Returns(tenantId);
        reportingClient
            .Setup(r => r.GetSettlementPayoutsAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Money.Gbp(0m));

        service = new ArtistDashboardService(
            concertModule.Object,
            reportingClient.Object,
            tenantContext.Object,
            timeProvider);
    }

    [Fact]
    public async Task GetKpisAsync_QueriesTenantMonthToDatePayouts_AndMapsToCents()
    {
        concertModule
            .Setup(m => m.GetArtistDashboardCountsAsync(tenantId, It.IsAny<CancellationToken>()))
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
    public async Task GetKpisAsync_CountsUnavailable_ReturnsNoneWithoutReportingCall()
    {
        concertModule
            .Setup(m => m.GetArtistDashboardCountsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Option.None<ArtistDashboardCounts>());

        var result = await service.GetKpisAsync();

        Assert.False(result.TryGetValue(out _));
        reportingClient.Verify(
            r => r.GetSettlementPayoutsAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetKpisAsync_AtMonthStart_ReturnsZeroWithoutReportingCall()
    {
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        concertModule
            .Setup(m => m.GetArtistDashboardCountsAsync(tenantId, It.IsAny<CancellationToken>()))
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
    public async Task GetKpisAsync_AtMonthStartWithoutTenant_ThrowsInvalidOperation()
    {
        timeProvider.SetUtcNow(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        tenantContext.SetupGet(t => t.TenantId).Returns((Guid?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetKpisAsync());
    }
}
