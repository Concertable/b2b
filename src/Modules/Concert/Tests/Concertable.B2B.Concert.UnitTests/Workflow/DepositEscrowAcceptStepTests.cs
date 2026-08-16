using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class DepositEscrowAcceptStepTests
{
    private readonly Mock<IBookingService> bookingService;
    private readonly Mock<IBus> bus;
    private readonly Mock<IDealAccessor> dealAccessor;
    private readonly DepositEscrowAcceptStep step;

    public DepositEscrowAcceptStepTests()
    {
        this.bookingService = new Mock<IBookingService>();
        this.bus = new Mock<IBus>();
        this.dealAccessor = new Mock<IDealAccessor>();
        this.step = new DepositEscrowAcceptStep(
            bookingService.Object,
            bus.Object,
            dealAccessor.Object,
            new Mock<ILogger<DepositEscrowAcceptStep>>().Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowInvalidOperation_WhenApplicationIsNotPrepaid()
    {
        var application = StandardApplication.Create(
            1,
            1,
            DealType.VenueHire,
            Guid.NewGuid(),
            Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => step.ExecuteAsync(application));
        bus.Verify(
            value => value.SendAsync(It.IsAny<DepositEscrowCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_StagesDepositWithStableApplicationOperationId()
    {
        var application = PrepaidApplication.Create(
            1,
            1,
            DealType.VenueHire,
            "pm_test",
            Guid.NewGuid(),
            Guid.NewGuid());
        this.bookingService
            .Setup(value => value.CreateStandardAsync(application))
            .ReturnsAsync(new StandardBookingDto(42));
        this.dealAccessor.SetupGet(value => value.Deal).Returns(new VenueHireDeal { HireFee = 12.34m });

        await this.step.ExecuteAsync(application);

        this.bus.Verify(value => value.SendAsync(
            It.Is<DepositEscrowCommand>(command =>
                command.OperationId == application.AcceptanceOperationId &&
                command.BookingId == 42 &&
                command.AmountMinor == 1234),
            It.IsAny<CancellationToken>()));
    }
}
