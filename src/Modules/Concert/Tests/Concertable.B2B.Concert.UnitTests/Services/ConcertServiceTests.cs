using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertServiceTests
{
    [Fact]
    public async Task DeclareDoorRevenueAsync_NegativeRevenue_MapsDomainFailureWithoutSaving()
    {
        var now = new DateTimeOffset(2026, 8, 10, 23, 0, 0, TimeSpan.Zero);
        var application = StandardApplication.Create(
            1,
            2,
            DealType.DoorSplit,
            Guid.NewGuid(),
            Guid.NewGuid());
        application.Transition(LifecycleState.Booked);
        var booking = DeferredBooking.Create(application.ToAccepted(), "pm_123");
        var period = new DateRange(now.UtcDateTime.AddHours(-3), now.UtcDateTime.AddHours(-1));
        var concert = ConcertEntity.CreateDraft(
            booking.ToConfirmed(2, period),
            "Concert",
            "About",
            []);
        var repository = new Mock<IConcertRepository>();
        repository
            .Setup(value => value.GetByIdForLifecycleAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(concert);
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(context => context.IsHost).Returns(true);
        var applicationRepository = new Mock<IApplicationRepository>();
        applicationRepository
            .Setup(value => value.GetLifecycleAndPaymentStateAsync(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LifecycleState.Booked, PaymentVerification.None));
        var service = new ConcertService(
            repository.Object,
            Mock.Of<IConcertReadRepository>(),
            Mock.Of<IInvoiceRepository>(),
            Mock.Of<IConcertValidator>(),
            Mock.Of<ICurrentUser>(),
            Mock.Of<IApplicationValidator>(),
            applicationRepository.Object,
            Mock.Of<IConcertDraftService>(),
            Mock.Of<ICancelExecutor>(),
            new FakeTimeProvider(now),
            tenantContext.Object);

        var result = await service.DeclareDoorRevenueAsync(42, -0.01m);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<DeclareDoorRevenueError.Negative>(error);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
