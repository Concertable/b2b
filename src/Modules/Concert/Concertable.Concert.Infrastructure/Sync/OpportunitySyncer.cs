using Concertable.Concert.Domain.Entities;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Application.Diffing;

namespace Concertable.Concert.Infrastructure.Sync;

internal sealed class OpportunitySyncer
    : CollectionSyncer<OpportunityEntity, OpportunityRequest>, IOpportunitySyncer
{
    private readonly IContractModule contractModule;

    public OpportunitySyncer(IBaseRepository<OpportunityEntity> repo, IContractModule contractModule)
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
        await contractModule.UpdateAsync(entity.ContractId, dto.Contract);
        entity.Update(new DateRange(dto.StartDate, dto.EndDate), entity.ContractId, dto.Genres);
    }
}
