using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Artist.Infrastructure.Services;
using Concertable.B2B.Concert.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.B2B.Artist.UnitTests.Services;

public sealed class ArtistDashboardServiceTests
{
    private readonly Mock<IArtistService> artistService = new();
    private readonly Mock<IConcertModule> concertModule = new();
    private readonly Mock<IManagerPaymentReportingClient> reportingClient = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 8, 13, 10, 30, 0, TimeSpan.Zero));
    private readonly Guid tenantId = Guid.NewGuid();
    private readonly ArtistDashboardService service;

    public ArtistDashboardServiceTests()
    {
        artistService.Setup(s => s.GetIdForCurrentUserAsync()).ReturnsAsync(42);
        tenantContext.SetupGet(t => t.TenantId).Returns(tenantId);
        reportingClient
            .Setup(r => r.GetSettlementPayoutsAsync(It.IsAny<Guid>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Money.Gbp(0m));

        service = new ArtistDashboardService(
            artistService.Object,
            concertModule.Object,
            reportingClient.Object,
            tenantContext.Object,
            timeProvider);
    }

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

        Assert.NotNull(result);
        Assert.Equal(6789, result!.MtdPayoutsCents);
        Assert.Equal(4, result.PendingApplications);
        Assert.Equal(tenantId, capturedPayee);
        Assert.Equal(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), capturedPeriod!.Start);
        Assert.Equal(timeProvider.GetUtcNow().UtcDateTime, capturedPeriod.End);
    }

    [Fact]
    public async Task GetKpisAsync_ReturnsNull_WhenCountsUnavailable()
    {
        concertModule
            .Setup(m => m.GetArtistDashboardCountsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArtistDashboardCounts?)null);

        var result = await service.GetKpisAsync();

        Assert.Null(result);
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

        Assert.NotNull(result);
        Assert.Equal(0, result.MtdPayoutsCents);
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
}
