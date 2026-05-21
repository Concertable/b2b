using Concertable.Contract.Application.Interfaces;
using Concertable.Shared.Exceptions;

namespace Concertable.Contract.Application.Services;

internal sealed class ContractService(
    IContractRepository contractRepository,
    IContractMapper mapper,
    IContractUpdater updater) : IContractService
{
    public async Task<IContract?> GetByIdAsync(int contractId, CancellationToken ct = default)
    {
        var entity = await contractRepository.GetByIdAsync(contractId);
        return entity is null ? null : mapper.ToContract(entity);
    }

    public async Task<IEnumerable<IContract>> GetByIdsAsync(IEnumerable<int> contractIds, CancellationToken ct = default)
    {
        var entities = await contractRepository.GetByIdsAsync(contractIds, ct);
        return entities.Select(mapper.ToContract);
    }

    public async Task<int> CreateAsync(IContract contract, CancellationToken ct = default)
    {
        var entity = mapper.ToEntity(contract);
        await contractRepository.AddAsync(entity);
        await contractRepository.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(int contractId, IContract contract, CancellationToken ct = default)
    {
        var existing = await contractRepository.GetByIdAsync(contractId)
            ?? throw new NotFoundException($"Contract {contractId} not found");

        updater.Apply(existing, contract);
        contractRepository.Update(existing);
        await contractRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(int contractId, CancellationToken ct = default)
    {
        var existing = await contractRepository.GetByIdAsync(contractId);
        if (existing is null) return;

        contractRepository.Remove(existing);
        await contractRepository.SaveChangesAsync();
    }
}
