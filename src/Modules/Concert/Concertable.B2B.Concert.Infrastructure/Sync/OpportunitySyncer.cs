using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Application.Diffing;

namespace Concertable.B2B.Concert.Infrastructure.Sync;

internal sealed class OpportunitySyncer
    : CollectionSyncer<OpportunityEntity, OpportunityRequest>, IOpportunitySyncer
{
    private readonly IDealModule dealModule;

    public OpportunitySyncer(IBaseRepository<OpportunityEntity> repo, IDealModule dealModule)
        : base(repo)
    {
        this.dealModule = dealModule;
    }

    protected override async Task<OpportunityEntity> CreateAsync(int venueId, OpportunityRequest dto)
    {
        var dealId = await dealModule.CreateAsync(dto.Deal);
        return OpportunityEntity.Create(
            venueId,
            new DateRange(dto.StartDate, dto.EndDate),
            dealId,
            dto.Genres);
    }

    protected override async Task UpdateAsync(OpportunityEntity entity, OpportunityRequest dto)
    {
        await dealModule.UpdateAsync(entity.DealId, dto.Deal);
        entity.Update(new DateRange(dto.StartDate, dto.EndDate), entity.DealId, dto.Genres);
    }
}
