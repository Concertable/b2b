namespace Concertable.B2B.Deal.Application.Interfaces;

internal interface IDealService
{
    Task<IDeal?> GetByIdAsync(int dealId, CancellationToken ct = default);
    Task<IEnumerable<IDeal>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default);
    Task<int> CreateAsync(IDeal deal, CancellationToken ct = default);
    Task UpdateAsync(int dealId, IDeal deal, CancellationToken ct = default);
    Task DeleteAsync(int dealId, CancellationToken ct = default);
}
