using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Contracts.Errors;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Infrastructure;

internal sealed class DealModule : IDealModule
{
    private readonly IDealService dealService;

    public DealModule(IDealService dealService)
    {
        this.dealService = dealService;
    }

    public Task<Option<DealDto>> GetByIdAsync(int dealId, CancellationToken ct = default)
        => dealService.FindByIdAsync(dealId, ct);

    public Task<IReadOnlyList<DealDto>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default)
        => dealService.GetByIdsAsync(dealIds, ct);

    public UnitResult<ValidationErrors> Validate(DealDto deal)
        => dealService.Validate(deal);

    public Task<Result<int, CreateDealError>> CreateAsync(DealDto deal, CancellationToken ct = default)
        => dealService.CreateAsync(deal, ct);

    public Task<UnitResult<UpdateDealError>> UpdateAsync(int dealId, DealDto deal, CancellationToken ct = default)
        => dealService.UpdateAsync(dealId, deal, ct);

    public Task DeleteAsync(int dealId, CancellationToken ct = default)
        => dealService.DeleteAsync(dealId, ct);
}
