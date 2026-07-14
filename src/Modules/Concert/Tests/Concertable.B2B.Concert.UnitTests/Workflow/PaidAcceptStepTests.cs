using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class PaidAcceptStepTests
{
    private const int ApplicationId = 1;
    private const string PaymentMethodId = "pm_card_visa";
    private readonly DoorSplitDeal deal = new() { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70 };

    private readonly Mock<IBookingService> bookingService;
    private readonly Mock<IDealAccessor> dealAccessor;
    private readonly PaidAcceptStep step;

    public PaidAcceptStepTests()
    {
        this.bookingService = new Mock<IBookingService>();
        this.dealAccessor = new Mock<IDealAccessor>();

        dealAccessor.SetupGet(c => c.Deal).Returns(deal);

        this.step = new PaidAcceptStep(bookingService.Object, dealAccessor.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateDeferredBooking_WhenAcceptable()
    {
        // Act
        await step.ExecuteAsync(ApplicationId, PaymentMethodId);

        // Assert
        bookingService.Verify(b => b.CreateDeferredAsync(ApplicationId, deal.DealType, PaymentMethodId), Times.Once);
    }
}
