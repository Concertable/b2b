using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Contracts.Errors;
using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;

namespace Concertable.B2B.Deal.Infrastructure;

internal sealed class DealModule : IDealModule
{
    private readonly IDealService dealService;

    public DealModule(IDealService dealService)
    {
        this.dealService = dealService;
    }

    public Task<Option<IDeal>> GetByIdAsync(int dealId, CancellationToken ct = default)
        => dealService.GetByIdAsync(dealId, ct);

    public Task<IReadOnlyList<IDeal>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default)
        => dealService.GetByIdsAsync(dealIds, ct);

    public UnitResult<ValidationErrors> Validate(IDeal deal)
        => dealService.Validate(deal);

    public Task<Result<int, CreateDealError>> CreateAsync(IDeal deal, CancellationToken ct = default)
        => dealService.CreateAsync(deal, ct);

    public Task<UnitResult<UpdateDealError>> UpdateAsync(int dealId, IDeal deal, CancellationToken ct = default)
        => dealService.UpdateAsync(dealId, deal, ct);

    public Task DeleteAsync(int dealId, CancellationToken ct = default)
        => dealService.DeleteAsync(dealId, ct);
}
