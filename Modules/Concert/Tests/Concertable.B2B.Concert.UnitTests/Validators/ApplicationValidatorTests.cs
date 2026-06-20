using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Validators;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Validators;

public sealed class ApplicationValidatorTests
{
    private const int ApplicationId = 1;
    private const int OpportunityId = 1;
    private const int VenueId = 1;
    private const int ArtistId = 1;
    private const int ContractId = 1;

    private readonly Guid venueTenantId = Guid.NewGuid();

    private readonly FakeTimeProvider timeProvider;
    private readonly Mock<IConcertAvailability> availability;
    private readonly Mock<IOpportunityRepository> opportunityRepository;
    private readonly Mock<IApplicationRepository> applicationRepository;
    private readonly Mock<IArtistModule> artistModule;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly ApplicationValidator validator;

    private DateRange FuturePeriod => new(timeProvider.GetUtcNow().AddDays(28).UtcDateTime, timeProvider.GetUtcNow().AddDays(28).AddHours(3).UtcDateTime);
    private DateRange PastPeriod => new(timeProvider.GetUtcNow().AddDays(-33).UtcDateTime, timeProvider.GetUtcNow().AddDays(-33).AddHours(3).UtcDateTime);

    public ApplicationValidatorTests()
    {
        this.timeProvider = new FakeTimeProvider();
        this.availability = new Mock<IConcertAvailability>();
        this.opportunityRepository = new Mock<IOpportunityRepository>();
        this.applicationRepository = new Mock<IApplicationRepository>();
        this.artistModule = new Mock<IArtistModule>();
        this.tenantContext = new Mock<ITenantContext>();

        tenantContext.SetupGet(t => t.TenantId).Returns(venueTenantId);
        opportunityRepository.Setup(r => r.GetByApplicationIdAsync(ApplicationId)).ReturnsAsync(Opportunity(FuturePeriod));
        applicationRepository.Setup(r => r.GetByIdAsync(ApplicationId)).ReturnsAsync(StandardApplication.Create(ArtistId, OpportunityId));

        this.validator = new ApplicationValidator(
            availability.Object,
            opportunityRepository.Object,
            applicationRepository.Object,
            artistModule.Object,
            tenantContext.Object,
            timeProvider);
    }

    private OpportunityEntity Opportunity(DateRange period)
    {
        var opportunity = OpportunityEntity.Create(VenueId, period, ContractId);
        opportunity.TenantId = venueTenantId;
        return opportunity;
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldSucceed_WhenAllRulesPass()
    {
        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenCallerDoesNotOwnOpportunity()
    {
        // Arrange
        tenantContext.SetupGet(t => t.TenantId).Returns(Guid.NewGuid());

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("You do not own this concert opportunity", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenOpportunityHasPassed()
    {
        // Arrange
        opportunityRepository.Setup(r => r.GetByApplicationIdAsync(ApplicationId)).ReturnsAsync(Opportunity(PastPeriod));

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("This concert opportunity has already passed", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenOpportunityAlreadyHasConcert()
    {
        // Arrange
        availability.Setup(r => r.OpportunityHasConcertAsync(It.IsAny<int>())).ReturnsAsync(true);

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("This concert opportunity already has a concert booked", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenArtistAlreadyHasConcertOnDate()
    {
        // Arrange
        availability.Setup(r => r.ArtistHasConcertOnDateAsync(ArtistId, FuturePeriod.Start)).ReturnsAsync(true);

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("This artist already has a concert on this day", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenVenueAlreadyHasConcertOnDate()
    {
        // Arrange
        availability.Setup(r => r.VenueHasConcertOnDateAsync(VenueId, FuturePeriod.Start)).ReturnsAsync(true);

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("You already have a concert on this day", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenOpportunityDoesNotExist()
    {
        // Arrange
        opportunityRepository.Setup(r => r.GetByApplicationIdAsync(ApplicationId)).ReturnsAsync((OpportunityEntity?)null);

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("Concert opportunity does not exist", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenApplicationDoesNotExist()
    {
        // Arrange
        applicationRepository.Setup(r => r.GetByIdAsync(ApplicationId)).ReturnsAsync((ApplicationEntity?)null);

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("Concert application does not exist", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanApplyAsync_ShouldFail_WhenUserHasNoArtistAccount()
    {
        // Arrange
        artistModule.Setup(m => m.GetIdForCurrentTenantAsync()).ReturnsAsync((int?)null);

        // Act
        var result = await validator.CanApplyAsync(OpportunityId);

        // Assert
        Assert.Equal("You must have an artist account to apply for a concert opportunity", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanApplyAsync_ShouldFail_WhenOpportunityDoesNotExist()
    {
        // Arrange
        artistModule.Setup(m => m.GetIdForCurrentTenantAsync()).ReturnsAsync(ArtistId);
        opportunityRepository.Setup(r => r.GetByIdAsync(OpportunityId)).ReturnsAsync((OpportunityEntity?)null);

        // Act
        var result = await validator.CanApplyAsync(OpportunityId);

        // Assert
        Assert.Equal("Concert opportunity does not exist", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanApplyAsync_ShouldSucceed_WhenUserHasArtistAndAllRulesPass()
    {
        // Arrange
        artistModule.Setup(m => m.GetIdForCurrentTenantAsync()).ReturnsAsync(ArtistId);
        opportunityRepository.Setup(r => r.GetByIdAsync(OpportunityId)).ReturnsAsync(Opportunity(FuturePeriod));

        // Act
        var result = await validator.CanApplyAsync(OpportunityId);

        // Assert
        Assert.True(result.IsSuccess);
    }
}
