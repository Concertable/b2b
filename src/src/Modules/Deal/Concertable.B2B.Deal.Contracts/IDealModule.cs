namespace Concertable.B2B.Deal.Contracts;

public interface IDealModule
{
    Task<IDeal?> GetByIdAsync(int contractId, CancellationToken ct = default);
    Task<IEnumerable<IDeal>> GetByIdsAsync(IEnumerable<int> contractIds, CancellationToken ct = default);
    Task<int> CreateAsync(IDeal contract, CancellationToken ct = default);
    Task UpdateAsync(int contractId, IDeal contract, CancellationToken ct = default);
    Task DeleteAsync(int contractId, CancellationToken ct = default);
}
