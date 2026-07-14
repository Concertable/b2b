namespace Concertable.B2B.Deal.Application.Interfaces;

internal interface IDealService
{
    Task<IDeal?> GetByIdAsync(int contractId, CancellationToken ct = default);
    Task<IEnumerable<IDeal>> GetByIdsAsync(IEnumerable<int> contractIds, CancellationToken ct = default);
    Task<int> CreateAsync(IDeal contract, CancellationToken ct = default);
    Task UpdateAsync(int contractId, IDeal contract, CancellationToken ct = default);
    Task DeleteAsync(int contractId, CancellationToken ct = default);
}
