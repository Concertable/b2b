namespace Concertable.Contract.Contracts;

public interface IContractModule
{
    Task<IContract?> GetByIdAsync(int contractId, CancellationToken ct = default);
    Task<IEnumerable<IContract>> GetByIdsAsync(IEnumerable<int> contractIds, CancellationToken ct = default);
    Task<int> CreateAsync(IContract contract, CancellationToken ct = default);
    Task UpdateAsync(int contractId, IContract contract, CancellationToken ct = default);
    Task DeleteAsync(int contractId, CancellationToken ct = default);
}
