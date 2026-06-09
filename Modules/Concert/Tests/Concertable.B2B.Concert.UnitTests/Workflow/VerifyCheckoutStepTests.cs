using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class VerifyCheckoutStepTests
{
    private const int ApplicationId = 1;
    private readonly Guid venueManagerId = Guid.NewGuid();
    private readonly PayeeSummary artist = new("Artist", "artist@example.com");
    private readonly CheckoutSession session = new("seti_secret", "cs", "cus");
    private readonly DoorSplitContract contract = new() { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70 };
    private readonly DoorSharePayment amount = new(70);

    private readonly Mock<IPayerLookup> payerLookup;
    private readonly Mock<IContractAccessor> contractAccessor;
    private readonly Mock<IManagerPaymentClient> managerPaymentClient;
    private readonly Mock<IPaymentAmountMapper> paymentAmountMapper;
    private readonly Mock<ITenantModule> tenantModule;
    private readonly VerifyCheckoutStep step;

    private IDictionary<string, string>? capturedMetadata;

    public VerifyCheckoutStepTests()
    {
        this.payerLookup = new Mock<IPayerLookup>();
        this.contractAccessor = new Mock<IContractAccessor>();
        this.managerPaymentClient = new Mock<IManagerPaymentClient>();
        this.paymentAmountMapper = new Mock<IPaymentAmountMapper>();
        this.tenantModule = new Mock<ITenantModule>();

        payerLookup.Setup(p => p.GetArtistAsync(ApplicationId)).ReturnsAsync(artist);
        payerLookup.Setup(p => p.GetVenueManagerIdAsync(ApplicationId)).ReturnsAsync(venueManagerId);
        contractAccessor.SetupGet(c => c.Contract).Returns(contract);
        paymentAmountMapper.Setup(m => m.ToPaymentAmount(contract)).Returns(amount);
        tenantModule
            .Setup(m => m.GetTenantIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid u, CancellationToken _) => u);
        managerPaymentClient
            .Setup(c => c.CreateVerifySessionAsync(It.IsAny<Guid>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IDictionary<string, string>, CancellationToken>((_, m, _) => capturedMetadata = m)
            .ReturnsAsync(session);

        this.step = new VerifyCheckoutStep(payerLookup.Object, contractAccessor.Object, managerPaymentClient.Object, paymentAmountMapper.Object, tenantModule.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldVerifyTheCardAndReturnSettlementCheckout()
    {
        // Act
        var checkout = await step.ExecuteAsync(ApplicationId);

        // Assert
        Assert.Equal(CheckoutLabels.Settlement, checkout.Labels);
        Assert.Same(amount, checkout.Amount);
        Assert.Equal(artist, checkout.Payee);
        Assert.Equal(session, checkout.Session);
        managerPaymentClient.Verify(
            c => c.CreateVerifySessionAsync(venueManagerId, It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(TransactionTypes.Verify, capturedMetadata!["type"]);
        Assert.Equal(ApplicationId.ToString(), capturedMetadata["applicationId"]);
        Assert.Equal(venueManagerId.ToString(), capturedMetadata["venueManagerId"]);
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
