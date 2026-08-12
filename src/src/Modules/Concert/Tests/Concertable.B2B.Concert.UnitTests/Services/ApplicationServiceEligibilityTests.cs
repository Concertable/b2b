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
    private readonly Mock<IArtistModule> artistModule;
    private readonly ApplicationService service;
    private readonly OpportunityEntity opportunity;
    private readonly ApplicationEntity application;

    public ApplicationServiceEligibilityTests()
    {
        this.repository = new Mock<IApplicationRepository>();
        this.validator = new Mock<IApplicationValidator>();
        this.opportunityRepository = new Mock<IOpportunityRepository>();
        this.artistModule = new Mock<IArtistModule>();
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
            Mock.Of<IApplicationNotifier>(),
            Mock.Of<IOpportunityService>(),
            this.opportunityRepository.Object,
            this.artistModule.Object,
            Mock.Of<IApplicationExecutor>(),
            Mock.Of<ICheckoutDispatcher>(),
            Mock.Of<IApplicationMapper>());
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

    private static ValidationResult InvalidApplication() =>
        ValidationResult.Invalid(new ValidationErrors(
            new Dictionary<string, string[]> { ["application"] = ["Validation failed."] }));
}
