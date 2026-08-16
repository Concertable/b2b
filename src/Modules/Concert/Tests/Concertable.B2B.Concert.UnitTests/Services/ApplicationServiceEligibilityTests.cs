using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Kernel.ValueObjects;
using Moq;
using Reunion;
using Reunion.Validation;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ApplicationServiceEligibilityTests
{
    private const int OpportunityId = 1;
    private const int ApplicationId = 2;
    private const int ArtistId = 3;

    private readonly Mock<IApplicationRepository> repository;
    private readonly Mock<IApplicationValidator> validator;
    private readonly Mock<IOpportunityRepository> opportunityRepository;
    private readonly Mock<IOpportunityService> opportunityService;
    private readonly Mock<IArtistModule> artistModule;
    private readonly Mock<IApplicationExecutor> executor;
    private readonly Mock<IApplicationNotifier> notifier;
    private readonly Mock<ICheckoutDispatcher> checkoutDispatcher;
    private readonly Mock<IApplicationMapper> mapper;
    private readonly ApplicationService service;
    private readonly OpportunityEntity opportunity;
    private readonly ApplicationEntity application;

    public ApplicationServiceEligibilityTests()
    {
        this.repository = new Mock<IApplicationRepository>();
        this.validator = new Mock<IApplicationValidator>();
        this.opportunityRepository = new Mock<IOpportunityRepository>();
        this.opportunityService = new Mock<IOpportunityService>();
        this.artistModule = new Mock<IArtistModule>();
        this.executor = new Mock<IApplicationExecutor>();
        this.notifier = new Mock<IApplicationNotifier>();
        this.checkoutDispatcher = new Mock<ICheckoutDispatcher>();
        this.mapper = new Mock<IApplicationMapper>();
        this.opportunity = OpportunityEntity.Create(
            1,
            new DateRange(
                new DateTime(2026, 9, 1, 20, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 9, 1, 23, 0, 0, DateTimeKind.Utc)),
            1);
        this.opportunity.TenantId = Guid.NewGuid();
        this.application = StandardApplication.Create(
            ArtistId,
            OpportunityId,
            DealType.FlatFee,
            this.opportunity.TenantId,
            Guid.NewGuid());

        this.artistModule
            .Setup(module => module.GetIdForCurrentTenantAsync())
            .ReturnsAsync(Option.Some(ArtistId));
        this.opportunityRepository
            .Setup(value => value.GetByIdAsync(OpportunityId))
            .ReturnsAsync(this.opportunity);
        this.opportunityRepository
            .Setup(value => value.GetByApplicationIdAsync(ApplicationId))
            .ReturnsAsync(this.opportunity);
        this.repository
            .Setup(value => value.GetByIdAsync(ApplicationId))
            .ReturnsAsync(this.application);
        this.validator
            .Setup(value => value.CanApplyAsync(this.opportunity, ArtistId))
            .ReturnsAsync(ValidationResult.Valid());
        this.validator
            .Setup(value => value.CanAcceptAsync(this.opportunity, this.application))
            .ReturnsAsync(ValidationResult.Valid());

        this.service = new ApplicationService(
            this.repository.Object,
            this.validator.Object,
            this.notifier.Object,
            this.opportunityService.Object,
            this.opportunityRepository.Object,
            this.artistModule.Object,
            this.executor.Object,
            this.checkoutDispatcher.Object,
            this.mapper.Object);
    }

    [Fact]
    public async Task CanApplyAsync_EligibleApplication_ReturnsTrue()
    {
        var result = await this.service.CanApplyAsync(OpportunityId);

        Assert.True(result);
    }

    [Fact]
    public async Task CanApplyAsync_MissingArtist_ReturnsFalse()
    {
        this.artistModule
            .Setup(module => module.GetIdForCurrentTenantAsync())
            .ReturnsAsync(Option.None<int>());

        var result = await this.service.CanApplyAsync(OpportunityId);

        Assert.False(result);
        this.validator.Verify(
            value => value.CanApplyAsync(It.IsAny<OpportunityEntity>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CanAcceptAsync_EligibleApplication_ReturnsTrue()
    {
        var result = await this.service.CanAcceptAsync(ApplicationId);

        Assert.True(result);
    }

    [Fact]
    public async Task CanAcceptAsync_MissingApplication_ReturnsFalse()
    {
        this.repository
            .Setup(value => value.GetByIdAsync(ApplicationId))
            .ReturnsAsync((ApplicationEntity?)null);

        var result = await this.service.CanAcceptAsync(ApplicationId);

        Assert.False(result);
        this.validator.Verify(
            value => value.CanAcceptAsync(It.IsAny<OpportunityEntity>(), It.IsAny<ApplicationEntity>()),
            Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_InvalidValidation_MapsStructuredErrors()
    {
        this.validator
            .Setup(value => value.CanApplyAsync(this.opportunity, ArtistId))
            .ReturnsAsync(InvalidApplication());

        var result = await this.service.ApplyAsync(
            OpportunityId,
            new ESignatureRequest { SignatoryName = "Test Signatory" });

        Assert.True(result.TryGetError(out var error));
        var invalid = Assert.IsType<ApplyApplicationError.Invalid>(error);
        Assert.Equal(["Validation failed."], invalid.Errors.Errors["application"]);
    }

    [Fact]
    public async Task ApplyAsync_ExecutorFailure_ReturnsErrorWithoutCompletingApplication()
    {
        var expected = new ApplyApplicationError.UnsupportedDeal(DealType.FlatFee);
        this.executor
            .Setup(value => value.ApplyAsync(
                OpportunityId,
                ArtistId,
                null,
                It.IsAny<ESignatureRequest>()))
            .ReturnsAsync(Result.Failure<ApplicationEntity, ApplyApplicationError>(expected));

        var result = await this.service.ApplyAsync(
            OpportunityId,
            new ESignatureRequest { SignatoryName = "Test Signatory" });

        Assert.True(result.TryGetError(out var error));
        Assert.Same(expected, error);
        this.notifier.Verify(value => value.AppliedAsync(It.IsAny<int>()), Times.Never);
        this.mapper.Verify(value => value.ToDtoAsync(It.IsAny<ApplicationEntity>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_InvalidValidation_MapsStructuredErrors()
    {
        this.validator
            .Setup(value => value.CanAcceptAsync(this.opportunity, this.application))
            .ReturnsAsync(InvalidApplication());

        var result = await this.service.AcceptAsync(
            ApplicationId,
            null,
            new ESignatureRequest { SignatoryName = "Test Signatory" });

        Assert.True(result.TryGetError(out var error));
        var ineligible = Assert.IsType<AcceptApplicationError.Ineligible>(error);
        var invalid = Assert.IsType<ApplicationEligibilityError.Invalid>(ineligible.Error);
        Assert.Equal(["Validation failed."], invalid.Errors.Errors["application"]);
    }

    [Fact]
    public async Task GetByOpportunityIdAsync_NotOwned_ReturnsForbidden()
    {
        this.opportunityService.Setup(value => value.OwnsOpportunityAsync(OpportunityId)).ReturnsAsync(false);

        var result = await this.service.GetByOpportunityIdAsync(OpportunityId);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ApplicationError.OpportunityForbidden>(error);
    }

    [Fact]
    public async Task GetPendingForArtistAsync_MissingArtist_ReturnsForbidden()
    {
        this.artistModule
            .Setup(module => module.GetIdForCurrentTenantAsync())
            .ReturnsAsync(Option.None<int>());

        var result = await this.service.GetPendingForArtistAsync();

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ApplicationError.MissingArtist>(error);
    }

    [Fact]
    public async Task ApplyCheckoutAsync_Ineligible_ReturnsTypedErrorWithoutDispatching()
    {
        this.artistModule
            .Setup(module => module.GetIdForCurrentTenantAsync())
            .ReturnsAsync(Option.None<int>());

        var result = await this.service.ApplyCheckoutAsync(OpportunityId);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ApplicationEligibilityError.MissingArtist>(error);
        this.checkoutDispatcher.Verify(
            value => value.ApplyCheckoutAsync(It.IsAny<int>()),
            Times.Never);
    }

    private static ValidationResult InvalidApplication() =>
        ValidationResult.Invalid(new ValidationErrors(
            new Dictionary<string, string[]> { ["application"] = ["Validation failed."] }));
}
