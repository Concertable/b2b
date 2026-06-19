using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.Kernel.Exceptions;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class VerifyCheckoutStepTests
{
    private const int ApplicationId = 1;
    private readonly Guid venueManagerId = Guid.NewGuid();
    private readonly Guid venueTenantId = Guid.NewGuid();
    private readonly PayeeSummary artist = new("Artist", "artist@example.com");
    private readonly CheckoutSession session = new("seti_secret", "cs", "cus");
    private readonly DoorSplitContract contract = new() { PaymentMethod = PaymentMethod.Cash, ArtistDoorPercent = 70 };
    private readonly DoorSharePayment amount = new(70);

    private readonly Mock<IApplicationRepository> applicationRepository;
    private readonly Mock<IContractAccessor> contractAccessor;
    private readonly Mock<IManagerPaymentClient> managerPaymentClient;
    private readonly Mock<IPaymentAmountMapper> paymentAmountMapper;
    private readonly VerifyCheckoutStep step;

    private IDictionary<string, string>? capturedMetadata;

    public VerifyCheckoutStepTests()
    {
        this.applicationRepository = new Mock<IApplicationRepository>();
        this.contractAccessor = new Mock<IContractAccessor>();
        this.managerPaymentClient = new Mock<IManagerPaymentClient>();
        this.paymentAmountMapper = new Mock<IPaymentAmountMapper>();

        applicationRepository.Setup(r => r.GetArtistPayeeAsync(ApplicationId)).ReturnsAsync(artist);
        applicationRepository.Setup(r => r.GetVenueManagerIdAsync(ApplicationId)).ReturnsAsync(venueManagerId);
        applicationRepository
            .Setup(r => r.GetVenueTenantIdAsync(ApplicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venueTenantId);
        contractAccessor.SetupGet(c => c.Contract).Returns(contract);
        paymentAmountMapper.Setup(m => m.ToPaymentAmount(contract)).Returns(amount);
        managerPaymentClient
            .Setup(c => c.CreateVerifySessionAsync(It.IsAny<Guid>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IDictionary<string, string>, CancellationToken>((_, m, _) => capturedMetadata = m)
            .ReturnsAsync(session);

        this.step = new VerifyCheckoutStep(applicationRepository.Object, contractAccessor.Object, managerPaymentClient.Object, paymentAmountMapper.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldVerifyTheCardAndReturnSettlementCheckout()
    {
        // Act
        var checkout = await step.ExecuteAsync(ApplicationId);

        /* Assert — the session targets the venue TENANT; the manager USER id rides the metadata
           so the failure webhook can notify them. */
        Assert.Equal(CheckoutLabels.Settlement, checkout.Labels);
        Assert.Same(amount, checkout.Amount);
        Assert.Equal(artist, checkout.Payee);
        Assert.Equal(session, checkout.Session);
        managerPaymentClient.Verify(
            c => c.CreateVerifySessionAsync(venueTenantId, It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(TransactionTypes.Verify, capturedMetadata!["type"]);
        Assert.Equal(ApplicationId.ToString(), capturedMetadata["applicationId"]);
        Assert.Equal(venueManagerId.ToString(), capturedMetadata["venueManagerId"]);
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
    public async Task ExecuteAsync_ShouldThrowNotFound_WhenVenueManagerMissing()
    {
        // Arrange
        applicationRepository.Setup(r => r.GetVenueManagerIdAsync(ApplicationId)).ReturnsAsync((Guid?)null);

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
