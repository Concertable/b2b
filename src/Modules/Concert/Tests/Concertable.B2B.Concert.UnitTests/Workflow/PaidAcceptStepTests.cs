using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class PaidAcceptStepTests
{
    private const string PaymentMethodId = "pm_card_visa";

    private readonly Mock<IBookingService> bookingService;
    private readonly PaidAcceptStep step;

    public PaidAcceptStepTests()
    {
        this.bookingService = new Mock<IBookingService>();
        this.step = new PaidAcceptStep(bookingService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateDeferredBooking_WhenAcceptable()
    {
        // Act
        var application = StandardApplication.Create(
            1,
            1,
            DealType.DoorSplit,
            Guid.NewGuid(),
            Guid.NewGuid());

        await step.ExecuteAsync(application, PaymentMethodId);

        // Assert
        bookingService.Verify(b => b.CreateDeferredAsync(application, PaymentMethodId), Times.Once);
    }
}
