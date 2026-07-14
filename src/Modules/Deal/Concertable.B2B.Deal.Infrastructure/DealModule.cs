using Concertable.B2B.Deal.Application.Interfaces;

namespace Concertable.B2B.Deal.Infrastructure;

internal sealed class DealModule : IDealModule
{
    private readonly IDealService dealService;

    public DealModule(IDealService dealService)
    {
        this.dealService = dealService;
    }

    public Task<IDeal?> GetByIdAsync(int dealId, CancellationToken ct = default)
        => dealService.GetByIdAsync(dealId, ct);

    public Task<IEnumerable<IDeal>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default)
        => dealService.GetByIdsAsync(dealIds, ct);

    public Task<int> CreateAsync(IDeal deal, CancellationToken ct = default)
        => dealService.CreateAsync(deal, ct);

    public Task UpdateAsync(int dealId, IDeal deal, CancellationToken ct = default)
        => dealService.UpdateAsync(dealId, deal, ct);

    public Task DeleteAsync(int dealId, CancellationToken ct = default)
        => dealService.DeleteAsync(dealId, ct);
}
