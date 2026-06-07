using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ReadModels;
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

    private readonly Guid venueOwnerId = Guid.NewGuid();

    private readonly FakeTimeProvider timeProvider;
    private readonly Mock<IConcertRepository> concertRepository;
    private readonly Mock<IOpportunityRepository> opportunityRepository;
    private readonly Mock<IApplicationRepository> applicationRepository;
    private readonly Mock<ICurrentUser> currentUser;
    private readonly ApplicationValidator validator;

    private DateRange FuturePeriod => new(timeProvider.GetUtcNow().AddDays(28).UtcDateTime, timeProvider.GetUtcNow().AddDays(28).AddHours(3).UtcDateTime);
    private DateRange PastPeriod => new(timeProvider.GetUtcNow().AddDays(-33).UtcDateTime, timeProvider.GetUtcNow().AddDays(-33).AddHours(3).UtcDateTime);

    public ApplicationValidatorTests()
    {
        this.timeProvider = new FakeTimeProvider();
        this.concertRepository = new Mock<IConcertRepository>();
        this.opportunityRepository = new Mock<IOpportunityRepository>();
        this.applicationRepository = new Mock<IApplicationRepository>();
        this.currentUser = new Mock<ICurrentUser>();

        currentUser.SetupGet(u => u.Id).Returns(venueOwnerId);
        opportunityRepository.Setup(r => r.GetByApplicationIdAsync(ApplicationId)).ReturnsAsync(Opportunity(FuturePeriod));
        applicationRepository.Setup(r => r.GetByIdAsync(ApplicationId)).ReturnsAsync(StandardApplication.Create(ArtistId, OpportunityId));

        this.validator = new ApplicationValidator(
            concertRepository.Object,
            opportunityRepository.Object,
            applicationRepository.Object,
            currentUser.Object,
            timeProvider);
    }

    private OpportunityEntity Opportunity(DateRange period)
    {
        var opportunity = OpportunityEntity.Create(VenueId, period, ContractId);
        opportunity.Venue = new VenueReadModel { Id = VenueId, UserId = venueOwnerId };
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
        currentUser.SetupGet(u => u.Id).Returns(Guid.NewGuid());

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
        concertRepository.Setup(r => r.OpportunityHasConcertAsync(It.IsAny<int>())).ReturnsAsync(true);

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("This concert opportunity already has a concert booked", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenArtistAlreadyHasConcertOnDate()
    {
        // Arrange
        concertRepository.Setup(r => r.ArtistHasConcertOnDateAsync(ArtistId, FuturePeriod.Start)).ReturnsAsync(true);

        // Act
        var result = await validator.CanAcceptAsync(ApplicationId);

        // Assert
        Assert.Equal("This artist already has a concert on this day", result.Errors.Single().Message);
    }

    [Fact]
    public async Task CanAcceptAsync_ShouldFail_WhenVenueAlreadyHasConcertOnDate()
    {
        // Arrange
        concertRepository.Setup(r => r.VenueHasConcertOnDateAsync(VenueId, FuturePeriod.Start)).ReturnsAsync(true);

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
}
