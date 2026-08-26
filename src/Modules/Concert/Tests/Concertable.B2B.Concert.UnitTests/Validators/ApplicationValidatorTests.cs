using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Validators;
using Concertable.Kernel.Identity;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Validators;

public sealed class ApplicationValidatorTests
{
    private const int OpportunityId = 1;
    private const int VenueId = 1;
    private const int ArtistId = 1;
    private const int DealId = 1;

    private readonly Guid venueTenantId = Guid.NewGuid();
    private readonly FakeTimeProvider timeProvider;
    private readonly Mock<IConcertAvailability> availability;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly ApplicationValidator validator;

    private DateRange FuturePeriod => new(
        this.timeProvider.GetUtcNow().AddDays(28).UtcDateTime,
        this.timeProvider.GetUtcNow().AddDays(28).AddHours(3).UtcDateTime);

    private DateRange PastPeriod => new(
        this.timeProvider.GetUtcNow().AddDays(-33).UtcDateTime,
        this.timeProvider.GetUtcNow().AddDays(-33).AddHours(3).UtcDateTime);

    public ApplicationValidatorTests()
    {
        this.timeProvider = new FakeTimeProvider();
        this.availability = new Mock<IConcertAvailability>();
        this.tenantContext = new Mock<ITenantContext>();

        this.tenantContext.SetupGet(context => context.TenantId).Returns(this.venueTenantId);

        this.validator = new ApplicationValidator(
            this.availability.Object,
            this.tenantContext.Object,
            this.timeProvider);
    }

    [Fact]
    public async Task CanApplyAsync_AllRulesPass_ReturnsValid()
    {
        var result = await this.validator.CanApplyAsync(this.Opportunity(this.FuturePeriod), ArtistId);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CanApplyAsync_MultipleRulesFail_AccumulatesOrderedStructuredErrors()
    {
        var opportunity = this.Opportunity(this.PastPeriod);
        this.availability
            .Setup(value => value.OpportunityHasConcertAsync(opportunity.Id))
            .ReturnsAsync(true);
        this.availability
            .Setup(value => value.ArtistHasConcertOnDateAsync(ArtistId, opportunity.Period.Start))
            .ReturnsAsync(true);

        var result = await this.validator.CanApplyAsync(opportunity, ArtistId);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            [
                "This concert opportunity has already passed",
                "This concert opportunity has already been booked for a concert",
                "You already have a concert on this day"
            ],
            errors.Errors["application"]);
    }

    [Fact]
    public async Task CanAcceptAsync_AllRulesPass_ReturnsValid()
    {
        var opportunity = this.Opportunity(this.FuturePeriod);
        var application = CreateApplication();

        var result = await this.validator.CanAcceptAsync(opportunity, application);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CanAcceptAsync_MultipleRulesFail_AccumulatesOrderedStructuredErrors()
    {
        var opportunity = this.Opportunity(this.PastPeriod);
        var application = CreateApplication();
        this.tenantContext.SetupGet(context => context.TenantId).Returns(Guid.NewGuid());
        this.availability
            .Setup(value => value.OpportunityHasConcertAsync(opportunity.Id))
            .ReturnsAsync(true);
        this.availability
            .Setup(value => value.ArtistHasConcertOnDateAsync(ArtistId, opportunity.Period.Start))
            .ReturnsAsync(true);
        this.availability
            .Setup(value => value.VenueHasConcertOnDateAsync(VenueId, opportunity.Period.Start))
            .ReturnsAsync(true);

        var result = await this.validator.CanAcceptAsync(opportunity, application);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            [
                "You do not own this concert opportunity",
                "This concert opportunity has already passed",
                "This concert opportunity already has a concert booked",
                "This artist already has a concert on this day",
                "You already have a concert on this day"
            ],
            errors.Errors["application"]);
    }

    [Fact]
    public async Task CanApplyAsync_AvailabilityThrows_PropagatesException()
    {
        var opportunity = this.Opportunity(this.FuturePeriod);
        var exception = new InvalidOperationException("Availability failed.");
        this.availability
            .Setup(value => value.OpportunityHasConcertAsync(opportunity.Id))
            .ThrowsAsync(exception);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => this.validator.CanApplyAsync(opportunity, ArtistId));

        Assert.Same(exception, thrown);
    }

    private OpportunityEntity Opportunity(DateRange period)
    {
        var opportunity = OpportunityEntity.Create(VenueId, period, DealId);
        opportunity.TenantId = this.venueTenantId;
        return opportunity;
    }

    private static ApplicationEntity CreateApplication() =>
        StandardApplication.Create(
            ArtistId,
            OpportunityId,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());
}
