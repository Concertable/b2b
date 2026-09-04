using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Reunion.Validation;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertServiceTests
{
    private static ConfirmedBookingSnapshot CreateBooking(DateTimeOffset now) => new(
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
        new DoorSplitTerms(50m));

    private static ConcertService CreateService(
        Mock<IConcertRepository> repository,
        Mock<IUnitOfWork> unitOfWork,
        DateTimeOffset now,
        IConcertValidator? validator = null) =>
        new(
            repository.Object,
            Mock.Of<IConcertReadRepository>(),
            Mock.Of<IInvoiceRepository>(),
            validator ?? Mock.Of<IConcertValidator>(),
            Mock.Of<IConcertWorkflow>(),
            Mock.Of<IArtistReadModelRepository>(),
            Mock.Of<IVenueReadModelRepository>(),
            Mock.Of<IBookingConfirmationEmailSender>(),
            Mock.Of<IBus>(),
            Mock.Of<IBookingModule>(),
            unitOfWork.Object,
            new FakeTimeProvider(now),
            Mock.Of<ITenantContext>(),
            Mock.Of<ILogger<ConcertService>>());

    [Fact]
    public async Task UpdateAsync_SaveRaceLost_ReturnsSuperseded()
    {
        var now = new DateTimeOffset(2026, 8, 10, 23, 0, 0, TimeSpan.Zero);
        var concert = ConcertEntity.CreateDraft(CreateBooking(now), "Concert", "About", []);
        var repository = new Mock<IConcertRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(value => value.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(concert);
        unitOfWork.Setup(value => value.TrySaveChangesAsync(
            It.IsAny<Func<DbUpdateException, bool>>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var validator = new Mock<IConcertValidator>();
        validator.Setup(value => value.CanUpdate(concert, It.IsAny<int>())).Returns(ValidationResult.Valid());
        var service = CreateService(repository, unitOfWork, now, validator.Object);

        var result = await service.UpdateAsync(
            42,
            new UpdateConcertRequest { Name = "Concert", About = "About", Price = 10m, TotalTickets = 100 });

        Assert.True(result.TryGetError(out var error));
        var superseded = Assert.IsType<UpdateConcertError.Superseded>(error);
        Assert.Equal(42, superseded.ConcertId);
    }

    [Fact]
    public async Task PostAsync_SaveRaceLost_ReturnsSuperseded()
    {
        var now = new DateTimeOffset(2026, 8, 10, 23, 0, 0, TimeSpan.Zero);
        var booking = CreateBooking(now);
        var concert = ConcertEntity.CreateDraft(booking, "Concert", "About", []);
        var persisted = ConcertEntity.CreateDraft(booking, "Concert", "About", []);
        var repository = new Mock<IConcertRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository
            .SetupSequence(value => value.GetByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(concert)
            .ReturnsAsync(persisted);
        unitOfWork.Setup(value => value.TrySaveChangesAsync(
            It.IsAny<Func<DbUpdateException, bool>>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var validator = new Mock<IConcertValidator>();
        validator.Setup(value => value.CanPost(concert)).Returns(ValidationResult.Valid());
        var service = CreateService(repository, unitOfWork, now, validator.Object);

        var result = await service.PostAsync(
            42,
            new UpdateConcertRequest { Name = "Concert", About = "About", Price = 10m, TotalTickets = 100 });

        Assert.True(result.TryGetError(out var error));
        var superseded = Assert.IsType<PostConcertError.Superseded>(error);
        Assert.Equal(42, superseded.ConcertId);
    }

    [Fact]
    public async Task DeclareDoorRevenueAsync_SaveRaceLost_ReturnsSuperseded()
    {
        var now = new DateTimeOffset(2026, 8, 10, 23, 0, 0, TimeSpan.Zero);
        var concert = ConcertEntity.CreateDraft(CreateBooking(now), "Concert", "About", []);
        var repository = new Mock<IConcertRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        repository.Setup(value => value.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(concert);
        unitOfWork.Setup(value => value.TrySaveChangesAsync(
            It.IsAny<Func<DbUpdateException, bool>>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(context => context.IsHost).Returns(true);
        var service = new ConcertService(
            repository.Object,
            Mock.Of<IConcertReadRepository>(),
            Mock.Of<IInvoiceRepository>(),
            Mock.Of<IConcertValidator>(),
            Mock.Of<IConcertWorkflow>(),
            Mock.Of<IArtistReadModelRepository>(),
            Mock.Of<IVenueReadModelRepository>(),
            Mock.Of<IBookingConfirmationEmailSender>(),
            Mock.Of<IBus>(),
            Mock.Of<IBookingModule>(),
            unitOfWork.Object,
            new FakeTimeProvider(now),
            tenantContext.Object,
            Mock.Of<ILogger<ConcertService>>());

        var result = await service.DeclareDoorRevenueAsync(42, 100m);

        Assert.True(result.TryGetError(out var error));
        var superseded = Assert.IsType<DeclareDoorRevenueError.Superseded>(error);
        Assert.Equal(42, superseded.ConcertId);
    }

    [Fact]
    public async Task DeclareDoorRevenueAsync_NegativeRevenue_MapsDomainFailureWithoutSaving()
    {
        var now = new DateTimeOffset(2026, 8, 10, 23, 0, 0, TimeSpan.Zero);
        var booking = new ConfirmedBookingSnapshot(
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
            new DoorSplitTerms(50m));
        var concert = ConcertEntity.CreateDraft(
            booking,
            "Concert",
            "About",
            []);
        var repository = new Mock<IConcertRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
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
            Mock.Of<IConcertWorkflow>(),
            Mock.Of<IArtistReadModelRepository>(),
            Mock.Of<IVenueReadModelRepository>(),
            Mock.Of<IBookingConfirmationEmailSender>(),
            Mock.Of<IBus>(),
            Mock.Of<IBookingModule>(),
            unitOfWork.Object,
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
