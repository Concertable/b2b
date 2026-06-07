using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.Kernel.Exceptions;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class DepositEscrowAcceptStepTests
{
    private const int ApplicationId = 1;

    private readonly Mock<IBookingService> bookingService;
    private readonly Mock<IEscrowClient> escrowClient;
    private readonly Mock<IPayerLookup> payerLookup;
    private readonly Mock<IContractAccessor> contractAccessor;
    private readonly Mock<IApplicationRepository> applicationRepository;
    private readonly DepositEscrowAcceptStep step;

    public DepositEscrowAcceptStepTests()
    {
        this.bookingService = new Mock<IBookingService>();
        this.escrowClient = new Mock<IEscrowClient>();
        this.payerLookup = new Mock<IPayerLookup>();
        this.contractAccessor = new Mock<IContractAccessor>();
        this.applicationRepository = new Mock<IApplicationRepository>();

        payerLookup.Setup(p => p.GetManagerIdsAsync(ApplicationId)).ReturnsAsync((Guid.NewGuid(), Guid.NewGuid()));

        this.step = new DepositEscrowAcceptStep(
            bookingService.Object,
            escrowClient.Object,
            payerLookup.Object,
            contractAccessor.Object,
            applicationRepository.Object,
            new Mock<ILogger<DepositEscrowAcceptStep>>().Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowBadRequest_WhenApplicationIsNotPrepaid()
    {
        // Arrange — a VenueHire accept requires a PrepaidApplication; a standard one must be rejected
        applicationRepository.Setup(r => r.GetByIdAsync(ApplicationId)).ReturnsAsync(StandardApplication.Create(1, 1));

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => step.ExecuteAsync(ApplicationId));
        escrowClient.Verify(
            c => c.DepositAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<PaymentSession>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
