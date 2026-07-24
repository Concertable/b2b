using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class SetupCheckoutStepTests
{
    private const int OpportunityId = 1;
    private readonly Guid venueManagerId = Guid.NewGuid();
    private readonly Guid artistTenantId = Guid.NewGuid();
    private readonly CheckoutSession session = new("seti_secret", "cs", "cus");
    private readonly VenueHireDeal deal = new() { PaymentMethod = PaymentMethod.Cash, HireFee = 300 };

    private readonly Mock<IOpportunityRepository> opportunityRepository;
    private readonly Mock<IUserModule> userModule;
    private readonly Mock<IDealAccessor> dealAccessor;
    private readonly Mock<IManagerPaymentClient> managerPaymentClient;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly SetupCheckoutStep step;

    private IDictionary<string, string>? capturedMetadata;

    public SetupCheckoutStepTests()
    {
        this.opportunityRepository = new Mock<IOpportunityRepository>();
        this.userModule = new Mock<IUserModule>();
        this.dealAccessor = new Mock<IDealAccessor>();
        this.managerPaymentClient = new Mock<IManagerPaymentClient>();
        this.tenantContext = new Mock<ITenantContext>();

        opportunityRepository
            .Setup(r => r.GetVenueSummaryByIdAsync(OpportunityId))
            .ReturnsAsync(("Venue", venueManagerId));
        userModule
            .Setup(m => m.GetManagerByIdAsync(venueManagerId))
            .ReturnsAsync(new ManagerDto { Id = venueManagerId, Email = "venue@example.com" });
        dealAccessor.SetupGet(c => c.Deal).Returns(deal);
        tenantContext.SetupGet(c => c.TenantId).Returns(artistTenantId);
        managerPaymentClient
            .Setup(c => c.CreateSetupSessionAsync(It.IsAny<Guid>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, IDictionary<string, string>, CancellationToken>((_, m, _) => capturedMetadata = m)
            .ReturnsAsync(session);

        this.step = new SetupCheckoutStep(
            opportunityRepository.Object, userModule.Object, dealAccessor.Object, managerPaymentClient.Object, tenantContext.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetUpTheArtistCardAndReturnChargeCheckout()
    {
        // Act
        var checkout = await step.ExecuteAsync(OpportunityId);

        // Assert — the session belongs to the applying artist's own TENANT (ambient context)
        Assert.Equal(CheckoutLabels.Charge, checkout.Labels);
        Assert.Equal(deal.HireFee, Assert.IsType<FlatPayment>(checkout.Amount).Amount);
        Assert.Equal(new PayeeSummary("Venue", "venue@example.com"), checkout.Payee);
        Assert.Equal(session, checkout.Session);
        managerPaymentClient.Verify(
            c => c.CreateSetupSessionAsync(artistTenantId, It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Equal(TransactionTypes.ApplicationApply, capturedMetadata![PaymentMetadataKeys.Type]);
        Assert.Equal(OpportunityId.ToString(), capturedMetadata[PaymentMetadataKeys.OpportunityId]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFound_WhenVenueMissing()
    {
        // Arrange
        opportunityRepository
            .Setup(r => r.GetVenueSummaryByIdAsync(OpportunityId))
            .ReturnsAsync(((string, Guid)?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => step.ExecuteAsync(OpportunityId));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowForbidden_WhenCurrentUserHasNoTenant()
    {
        // Arrange
        tenantContext.SetupGet(c => c.TenantId).Returns((Guid?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => step.ExecuteAsync(OpportunityId));
    }
}
