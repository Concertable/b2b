using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Deal.Application.Services;

internal sealed class DealService : IDealService
{
    private readonly IDealRepository contractRepository;
    private readonly IDealMapper mapper;
    private readonly IDealUpdater updater;

    public DealService(
        IDealRepository contractRepository,
        IDealMapper mapper,
        IDealUpdater updater)
    {
        this.contractRepository = contractRepository;
        this.mapper = mapper;
        this.updater = updater;
    }

    public async Task<IDeal?> GetByIdAsync(int contractId, CancellationToken ct = default)
    {
        var entity = await contractRepository.GetByIdAsync(contractId);
        return entity is null ? null : mapper.ToDeal(entity);
    }

    public async Task<IEnumerable<IDeal>> GetByIdsAsync(IEnumerable<int> contractIds, CancellationToken ct = default)
    {
        var entities = await contractRepository.GetByIdsAsync(contractIds, ct);
        return mapper.ToDeals(entities);
    }

    public async Task<int> CreateAsync(IDeal contract, CancellationToken ct = default)
    {
        var entity = mapper.ToEntity(contract);
        await contractRepository.AddAsync(entity);
        await contractRepository.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(int contractId, IDeal contract, CancellationToken ct = default)
    {
        var existing = await contractRepository.GetByIdAsync(contractId)
            .OrNotFound($"Contract {contractId}");

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
