using Concertable.B2B.Deal.Application.Interfaces;

namespace Concertable.B2B.Deal.Infrastructure;

internal sealed class DealModule : IDealModule
{
    private readonly IDealService contractService;

    public DealModule(IDealService contractService)
    {
        this.contractService = contractService;
    }

    public Task<IDeal?> GetByIdAsync(int contractId, CancellationToken ct = default)
        => contractService.GetByIdAsync(contractId, ct);

    public Task<IEnumerable<IDeal>> GetByIdsAsync(IEnumerable<int> contractIds, CancellationToken ct = default)
        => contractService.GetByIdsAsync(contractIds, ct);

    public Task<int> CreateAsync(IDeal contract, CancellationToken ct = default)
        => contractService.CreateAsync(contract, ct);

    public Task UpdateAsync(int contractId, IDeal contract, CancellationToken ct = default)
        => contractService.UpdateAsync(contractId, contract, ct);

    public Task DeleteAsync(int contractId, CancellationToken ct = default)
        => contractService.DeleteAsync(contractId, ct);
}
