using Concertable.B2B.Opportunity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Services;

internal sealed class OpportunityHandoffService : IOpportunityHandoffService
{
    private readonly IOpportunityReadDbContext dbContext;
    private readonly IOpportunityHandoffRepository repository;

    public OpportunityHandoffService(
        IOpportunityReadDbContext dbContext,
        IOpportunityHandoffRepository repository)
    {
        this.dbContext = dbContext;
        this.repository = repository;
    }

    public Task<OpportunityHandoffDto?> GetDetailsAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        dbContext.Opportunities
            .Where(opportunity => opportunity.Id == opportunityId)
            .ToHandoffDtos()
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<OpportunityHandoffDto>> GetDetailsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        await dbContext.Opportunities
            .Where(opportunity => opportunityIds.Contains(opportunity.Id))
            .ToHandoffDtos()
            .ToListAsync(ct);

    public Task<OpportunityHandoffDto?> GetOpenDetailsAsync(
        int opportunityId,
        CancellationToken ct = default) =>
        dbContext.Opportunities
            .Where(opportunity =>
                opportunity.Id == opportunityId &&
                opportunity.State == OpportunityState.Open)
            .ToHandoffDtos()
            .FirstOrDefaultAsync(ct);

    public Task<bool> TryClaimAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default) =>
        repository.TryClaimAsync(opportunityId, venueTenantId, ct);
}

internal static class OpportunityHandoffMappers
{
    extension(IQueryable<OpportunityEntity> opportunities)
    {
        public IQueryable<OpportunityHandoffDto> ToHandoffDtos() =>
            opportunities
            .Select(opportunity => new OpportunityHandoffDto(
                opportunity.Id,
                opportunity.VenueId,
                opportunity.TenantId,
                opportunity.DealId,
                opportunity.Period.Start,
                opportunity.Period.End,
                opportunity.Genres));
    }
}
