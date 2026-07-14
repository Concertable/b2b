using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Application.Diffing;

namespace Concertable.B2B.Concert.Infrastructure.Sync;

internal sealed class OpportunitySyncer
    : CollectionSyncer<OpportunityEntity, OpportunityRequest>, IOpportunitySyncer
{
    private readonly IDealModule contractModule;

    public OpportunitySyncer(IBaseRepository<OpportunityEntity> repo, IDealModule contractModule)
        : base(repo)
    {
        this.contractModule = contractModule;
    }

    protected override async Task<OpportunityEntity> CreateAsync(int venueId, OpportunityRequest dto)
    {
        var contractId = await contractModule.CreateAsync(dto.Contract);
        return OpportunityEntity.Create(
            venueId,
            new DateRange(dto.StartDate, dto.EndDate),
            contractId,
            dto.Genres);
    }

    protected override async Task UpdateAsync(OpportunityEntity entity, OpportunityRequest dto)
    {
        await contractModule.UpdateAsync(entity.DealId, dto.Contract);
        entity.Update(new DateRange(dto.StartDate, dto.EndDate), entity.DealId, dto.Genres);
    }
}
