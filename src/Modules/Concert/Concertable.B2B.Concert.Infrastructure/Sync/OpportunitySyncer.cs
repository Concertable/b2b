using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Application.Diffing;

namespace Concertable.B2B.Concert.Infrastructure.Sync;

internal sealed class OpportunitySyncer
    : CollectionSyncer<OpportunityEntity, OpportunityRequest>, IOpportunitySyncer
{
    private readonly IDealModule dealModule;

    public OpportunitySyncer(IWriteRepository<OpportunityEntity> repository, IDealModule dealModule)
        : base(repository)
    {
        this.dealModule = dealModule;
    }

    protected override async Task<OpportunityEntity> CreateAsync(int venueId, OpportunityRequest dto)
    {
        var result = await dealModule.CreateAsync(dto.Deal);
        var dealId = result.Match(
            id => id,
            _ => throw new InvalidOperationException("Deal creation failed after successful validation."));
        return OpportunityEntity.Create(
            venueId,
            new DateRange(dto.StartDate, dto.EndDate),
            dealId,
            dto.Genres);
    }

    protected override async Task UpdateAsync(OpportunityEntity entity, OpportunityRequest dto)
    {
        var result = await dealModule.UpdateAsync(entity.DealId, dto.Deal);
        if (result.IsFailure)
            throw new InvalidOperationException("Deal update failed after successful validation.");

        entity.Update(new DateRange(dto.StartDate, dto.EndDate), entity.DealId, dto.Genres);
    }
}
