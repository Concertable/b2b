using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.Kernel.ValueObjects;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class DepositEscrowAcceptStepTests
{
    private readonly Mock<IBookingService> bookingService;
    private readonly Mock<IEscrowOperationsClient> escrowClient;
    private readonly Mock<IDealAccessor> dealAccessor;
    private readonly DepositEscrowAcceptStep step;

    public DepositEscrowAcceptStepTests()
    {
        this.bookingService = new Mock<IBookingService>();
        this.escrowClient = new Mock<IEscrowOperationsClient>();
        this.dealAccessor = new Mock<IDealAccessor>();
        this.step = new DepositEscrowAcceptStep(
            bookingService.Object,
            escrowClient.Object,
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
        escrowClient.Verify(
            c => c.DepositAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Money>(), It.IsAny<string>(), It.IsAny<PaymentSession>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
