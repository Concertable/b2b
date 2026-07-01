using Concertable.B2B.Contract.Application.Interfaces;

namespace Concertable.B2B.Contract.Infrastructure;

internal sealed class ContractModule : IContractModule
{
    private readonly IContractService contractService;

    public ContractModule(IContractService contractService)
    {
        this.contractService = contractService;
    }

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
