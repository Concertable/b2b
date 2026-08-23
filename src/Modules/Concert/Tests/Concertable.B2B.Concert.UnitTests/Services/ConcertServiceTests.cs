using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertServiceTests
{
    [Fact]
    public async Task DeclareDoorRevenueAsync_NegativeRevenue_MapsDomainFailureWithoutSaving()
    {
        var now = new DateTimeOffset(2026, 8, 10, 23, 0, 0, TimeSpan.Zero);
        var booking = new ConfirmedBooking(
            Guid.NewGuid(),
            1,
            2,
            3,
            4,
            5,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DealType.DoorSplit,
            true,
            now.UtcDateTime.AddHours(-3),
            now.UtcDateTime.AddHours(-1),
            [],
            new DoorSplitBookingTerms(50m, "pm_123"));
        var concert = ConcertEntity.CreateDraft(
            booking,
            "Concert",
            "About",
            []);
        var repository = new Mock<IConcertRepository>();
        repository
            .Setup(value => value.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(concert);
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(context => context.IsHost).Returns(true);
        var service = new ConcertService(
            repository.Object,
            Mock.Of<IConcertReadRepository>(),
            Mock.Of<IInvoiceRepository>(),
            Mock.Of<IConcertValidator>(),
            Mock.Of<IArtistReadModelRepository>(),
            Mock.Of<IVenueReadModelRepository>(),
            Mock.Of<IConcertNotifier>(),
            Mock.Of<IBookingConfirmationEmailSender>(),
            Mock.Of<IBookingModule>(),
            new FakeTimeProvider(now),
            tenantContext.Object,
            Mock.Of<ILogger<ConcertService>>());

        var result = await service.DeclareDoorRevenueAsync(42, -0.01m);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<DeclareDoorRevenueError.Negative>(error);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
