using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Validators;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.Contracts.Enums;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationValidatorTests
{
    private readonly Guid venueTenantId;
    private readonly ApplicationValidator validator;

    public ApplicationValidatorTests()
    {
        venueTenantId = Guid.NewGuid();
        validator = new ApplicationValidator(
            new AvailableApplicationProjection(),
            new TestTenantContext(venueTenantId),
            TimeProvider.System);
    }

    [Fact]
    public async Task CanAcceptAsync_WithdrawnOpportunity_ReturnsInvalid()
    {
        var opportunity = new OpportunityDto(
            1,
            2,
            venueTenantId,
            3,
            DateTime.MaxValue,
            DateTime.MaxValue,
            new HashSet<Genre>(),
            false);
        var application = StandardApplication.Create(
            4,
            opportunity.Id,
            DealType.FlatFee,
            venueTenantId,
            Guid.NewGuid());

        var result = await validator.CanAcceptAsync(opportunity, application);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Contains(
            "This concert opportunity is no longer open",
            errors.ToDictionary()["application"]);
    }

    private sealed class AvailableApplicationProjection : IApplicationAvailabilityProjection
    {
        public Task<bool> OpportunityHasConcertAsync(
            int opportunityId,
            CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<bool> ArtistHasConcertOnDateAsync(
            int artistId,
            DateTime date,
            CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<bool> VenueHasConcertOnDateAsync(
            int venueId,
            DateTime date,
            CancellationToken ct = default) =>
            Task.FromResult(false);
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public TestTenantContext(Guid tenantId)
        {
            TenantId = tenantId;
        }

        public Guid? TenantId { get; }
        public bool IsHost => false;
    }
}
