using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.Opportunity.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Repositories;

internal sealed class OpportunityRepository : TenantScopedRepository<OpportunityEntity>, IOpportunityRepository
{
    private const int TenantOpportunitiesLockNamespace = 10241003;
    private readonly OpportunityDbContext context;
    private readonly TimeProvider timeProvider;
    private readonly UnitOfWorkAccessor unitOfWorkAccessor;

    public OpportunityRepository(
        OpportunityDbContext context,
        ITenantContext tenant,
        TimeProvider timeProvider,
        UnitOfWorkAccessor unitOfWorkAccessor)
        : base(context, tenant)
    {
        this.context = context;
        this.timeProvider = timeProvider;
        this.unitOfWorkAccessor = unitOfWorkAccessor;
    }

    public async Task AcquireTenantOpportunitiesSharedLockAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock_shared({TenantOpportunitiesLockNamespace}, hashtext({tenantId.ToString()}))",
            ct);
    }

    public async Task AcquireTenantOpportunitiesExclusiveLockAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnlistAsync(ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({TenantOpportunitiesLockNamespace}, hashtext({tenantId.ToString()}))",
            ct);
    }

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId) =>
        await context.Opportunities
            .ActiveForVenue(venueId, timeProvider.GetUtcNow().UtcDateTime)
            .ToListAsync();

    public Task<int?> GetDealIdByIdAsync(int opportunityId) =>
        context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => (int?)o.DealId)
            .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<OpportunityEntity>> GetByIdsAsync(IReadOnlyCollection<int> ids) =>
        await context.Opportunities
            .Where(o => ids.Contains(o.Id))
            .ToListAsync();

    private Task EnlistAsync(CancellationToken ct) =>
        (unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("A tenant opportunities lock requires a unit of work."))
            .EnlistAsync(context, ct);
}
