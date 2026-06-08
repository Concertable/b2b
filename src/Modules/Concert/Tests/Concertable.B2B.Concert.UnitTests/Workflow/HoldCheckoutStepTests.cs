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
    private readonly Guid venueManagerId = Guid.NewGuid();
    private readonly PayeeSummary artist = new("Artist", "artist@example.com");
    private readonly CheckoutSession session = new("pi_secret", "cs", "cus");
    private readonly FlatFeeContract contract = new() { PaymentMethod = PaymentMethod.Cash, Fee = 100 };

    private readonly Mock<IPayerLookup> payerLookup;
    private readonly Mock<IContractAccessor> contractAccessor;
    private readonly Mock<IManagerPaymentClient> managerPaymentClient;
    private readonly HoldCheckoutStep step;

    private IDictionary<string, string>? capturedMetadata;

    public HoldCheckoutStepTests()
    {
        this.payerLookup = new Mock<IPayerLookup>();
        this.contractAccessor = new Mock<IContractAccessor>();
        this.managerPaymentClient = new Mock<IManagerPaymentClient>();

        payerLookup.Setup(p => p.GetArtistAsync(ApplicationId)).ReturnsAsync(artist);
        payerLookup.Setup(p => p.GetVenueManagerIdAsync(ApplicationId)).ReturnsAsync(venueManagerId);
        contractAccessor.SetupGet(c => c.Contract).Returns(contract);
        managerPaymentClient
            .Setup(c => c.CreateHoldSessionAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, decimal, IDictionary<string, string>, CancellationToken>((_, _, m, _) => capturedMetadata = m)
            .ReturnsAsync(session);

        this.step = new HoldCheckoutStep(payerLookup.Object, contractAccessor.Object, managerPaymentClient.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHoldTheFeeAndReturnChargeCheckout()
    {
        // Act
        var checkout = await step.ExecuteAsync(ApplicationId);

        // Assert
        Assert.Equal(CheckoutLabels.Charge, checkout.Labels);
        Assert.Equal(contract.Fee, Assert.IsType<FlatPayment>(checkout.Amount).Amount);
        Assert.Equal(artist, checkout.Payee);
        Assert.Equal(session, checkout.Session);
        managerPaymentClient.Verify(
            c => c.CreateHoldSessionAsync(venueManagerId, contract.Fee, It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("applicationAccept", capturedMetadata!["type"]);
        Assert.Equal(ApplicationId.ToString(), capturedMetadata["applicationId"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFound_WhenArtistMissing()
    {
        // Arrange
        payerLookup.Setup(p => p.GetArtistAsync(ApplicationId)).ReturnsAsync((PayeeSummary?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => step.ExecuteAsync(ApplicationId));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFound_WhenVenueManagerMissing()
    {
        // Arrange
        payerLookup.Setup(p => p.GetVenueManagerIdAsync(ApplicationId)).ReturnsAsync((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => step.ExecuteAsync(ApplicationId));
    }
}
