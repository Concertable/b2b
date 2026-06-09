using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Payment.Client;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class SetupCheckoutStepTests
{
    private const int OpportunityId = 1;
    private readonly Guid artistManagerId = Guid.NewGuid();
    private readonly PayeeSummary venue = new("Venue", "venue@example.com");
    private readonly CheckoutSession session = new("seti_secret", "cs", "cus");
    private readonly VenueHireContract contract = new() { PaymentMethod = PaymentMethod.Cash, HireFee = 300 };

    private readonly Mock<IPayerLookup> payerLookup;
    private readonly Mock<IContractAccessor> contractAccessor;
    private readonly Mock<IManagerPaymentClient> managerPaymentClient;
    private readonly Mock<ICurrentUser> currentUser;
    private readonly Mock<ITenantModule> tenantModule;
    private readonly SetupCheckoutStep step;

    private IDictionary<string, string>? capturedMetadata;

    public SetupCheckoutStepTests()
    {
        this.payerLookup = new Mock<IPayerLookup>();
        this.contractAccessor = new Mock<IContractAccessor>();
        this.managerPaymentClient = new Mock<IManagerPaymentClient>();
        this.currentUser = new Mock<ICurrentUser>();
        this.tenantModule = new Mock<ITenantModule>();

        payerLookup.Setup(p => p.GetVenueByOpportunityIdAsync(OpportunityId)).ReturnsAsync(venue);
        contractAccessor.SetupGet(c => c.Contract).Returns(contract);
        currentUser.SetupGet(c => c.Id).Returns(artistManagerId);
        tenantModule
            .Setup(m => m.GetTenantIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid u, CancellationToken _) => u);
        managerPaymentClient
            .Setup(c => c.CreateSetupSessionAsync(It.IsAny<Guid>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IDictionary<string, string>, CancellationToken>((_, m, _) => capturedMetadata = m)
            .ReturnsAsync(session);

        this.step = new SetupCheckoutStep(payerLookup.Object, contractAccessor.Object, managerPaymentClient.Object, currentUser.Object, tenantModule.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetUpTheArtistCardAndReturnChargeCheckout()
    {
        // Act
        var checkout = await step.ExecuteAsync(OpportunityId);

        // Assert
        Assert.Equal(CheckoutLabels.Charge, checkout.Labels);
        Assert.Equal(contract.HireFee, Assert.IsType<FlatPayment>(checkout.Amount).Amount);
        Assert.Equal(venue, checkout.Payee);
        Assert.Equal(session, checkout.Session);
        managerPaymentClient.Verify(
            c => c.CreateSetupSessionAsync(artistManagerId, It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal("applicationApply", capturedMetadata!["type"]);
        Assert.Equal(OpportunityId.ToString(), capturedMetadata["opportunityId"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFound_WhenVenueMissing()
    {
        // Arrange
        payerLookup.Setup(p => p.GetVenueByOpportunityIdAsync(OpportunityId)).ReturnsAsync((PayeeSummary?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => step.ExecuteAsync(OpportunityId));
    }
}
