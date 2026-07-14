using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.Kernel.Exceptions;
using Concertable.Payment.Client;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class HoldCheckoutStepTests
{
    private const int ApplicationId = 1;
    private readonly Guid venueTenantId = Guid.NewGuid();
    private readonly PayeeSummary artist = new("Artist", "artist@example.com");
    private readonly CheckoutSession session = new("pi_secret", "cs", "cus");
    private readonly FlatFeeDeal deal = new() { PaymentMethod = PaymentMethod.Cash, Fee = 100 };

    private readonly Mock<IApplicationRepository> applicationRepository;
    private readonly Mock<IDealAccessor> dealAccessor;
    private readonly Mock<IManagerPaymentClient> managerPaymentClient;
    private readonly HoldCheckoutStep step;

    private IDictionary<string, string>? capturedMetadata;

    public HoldCheckoutStepTests()
    {
        this.applicationRepository = new Mock<IApplicationRepository>();
        this.dealAccessor = new Mock<IDealAccessor>();
        this.managerPaymentClient = new Mock<IManagerPaymentClient>();

        applicationRepository.Setup(r => r.GetArtistPayeeAsync(ApplicationId)).ReturnsAsync(artist);
        applicationRepository
            .Setup(r => r.GetVenueTenantIdAsync(ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venueTenantId);
        dealAccessor.SetupGet(c => c.Deal).Returns(deal);
        managerPaymentClient
            .Setup(c => c.CreateHoldSessionAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, decimal, IDictionary<string, string>, CancellationToken>((_, _, m, _) => capturedMetadata = m)
            .ReturnsAsync(session);

        this.step = new HoldCheckoutStep(applicationRepository.Object, dealAccessor.Object, managerPaymentClient.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHoldTheFeeAndReturnChargeCheckout()
    {
        // Act
        var checkout = await step.ExecuteAsync(ApplicationId);

        // Assert — the session is created against the venue TENANT from the application snapshot
        Assert.Equal(CheckoutLabels.Charge, checkout.Labels);
        Assert.Equal(deal.Fee, Assert.IsType<FlatPayment>(checkout.Amount).Amount);
        Assert.Equal(artist, checkout.Payee);
        Assert.Equal(session, checkout.Session);
        managerPaymentClient.Verify(
            c => c.CreateHoldSessionAsync(venueTenantId, deal.Fee, It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("applicationAccept", capturedMetadata!["type"]);
        Assert.Equal(ApplicationId.ToString(), capturedMetadata["applicationId"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFound_WhenArtistMissing()
    {
        // Arrange
        applicationRepository.Setup(r => r.GetArtistPayeeAsync(ApplicationId)).ReturnsAsync((PayeeSummary?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => step.ExecuteAsync(ApplicationId));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFound_WhenVenueTenantMissing()
    {
        // Arrange
        applicationRepository
            .Setup(r => r.GetVenueTenantIdAsync(ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => step.ExecuteAsync(ApplicationId));
    }
}
