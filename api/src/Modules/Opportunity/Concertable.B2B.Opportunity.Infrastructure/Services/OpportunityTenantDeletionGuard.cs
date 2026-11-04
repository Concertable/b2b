using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Opportunity.Infrastructure.Services;

internal sealed class OpportunityTenantDeletionGuard : ITenantDeletionGuard
{
    private readonly IOpportunityRepository opportunityRepository;
    private readonly IOpportunityReadRepository opportunityReadRepository;

    public OpportunityTenantDeletionGuard(
        IOpportunityRepository opportunityRepository,
        IOpportunityReadRepository opportunityReadRepository)
    {
        this.opportunityRepository = opportunityRepository;
        this.opportunityReadRepository = opportunityReadRepository;
    }

    public async Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default)
    {
        await opportunityRepository.AcquireTenantOpportunitiesExclusiveLockAsync(tenantId, ct);
        return await opportunityReadRepository.ExistsLiveByTenantIdAsync(tenantId, ct);
    }
}
