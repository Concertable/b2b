using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class PaidAcceptStepTests
{
    private const int ApplicationId = 1;
    private const string PaymentMethodId = "pm_card_visa";
    private readonly DoorSplitContract contract = new() { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70 };

    private readonly Mock<IBookingService> bookingService;
    private readonly Mock<IContractAccessor> contractAccessor;
    private readonly PaidAcceptStep step;

    public PaidAcceptStepTests()
    {
        this.bookingService = new Mock<IBookingService>();
        this.contractAccessor = new Mock<IContractAccessor>();

        contractAccessor.SetupGet(c => c.Contract).Returns(contract);

        this.step = new PaidAcceptStep(bookingService.Object, contractAccessor.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateDeferredBooking_WhenAcceptable()
    {
        // Act
        await step.ExecuteAsync(ApplicationId, PaymentMethodId);

        // Assert
        bookingService.Verify(b => b.CreateDeferredAsync(ApplicationId, contract.ContractType, PaymentMethodId), Times.Once);
    }
}
