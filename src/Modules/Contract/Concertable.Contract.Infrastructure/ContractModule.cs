using Concertable.Contract.Application.Interfaces;

namespace Concertable.Contract.Infrastructure;

internal sealed class ContractModule(IContractService contractService) : IContractModule
{
    public Task<IContract?> GetByIdAsync(int contractId, CancellationToken ct = default)
        => contractService.GetByIdAsync(contractId, ct);

    public Task<IEnumerable<IContract>> GetByIdsAsync(IEnumerable<int> contractIds, CancellationToken ct = default)
        => contractService.GetByIdsAsync(contractIds, ct);

    public Task<int> CreateAsync(IContract contract, CancellationToken ct = default)
        => contractService.CreateAsync(contract, ct);

    public Task UpdateAsync(int contractId, IContract contract, CancellationToken ct = default)
        => contractService.UpdateAsync(contractId, contract, ct);

    public Task DeleteAsync(int contractId, CancellationToken ct = default)
        => contractService.DeleteAsync(contractId, ct);
}
