using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Deal.Application.Services;

internal sealed class DealService : IDealService
{
    private readonly IDealRepository dealRepository;
    private readonly IDealMapper mapper;
    private readonly IDealUpdater updater;

    public DealService(
        IDealRepository dealRepository,
        IDealMapper mapper,
        IDealUpdater updater)
    {
        this.dealRepository = dealRepository;
        this.mapper = mapper;
        this.updater = updater;
    }

    public async Task<IDeal?> GetByIdAsync(int dealId, CancellationToken ct = default)
    {
        var entity = await dealRepository.GetByIdAsync(dealId);
        return entity is null ? null : mapper.ToDeal(entity);
    }

    public async Task<IEnumerable<IDeal>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default)
    {
        var entities = await dealRepository.GetByIdsAsync(dealIds, ct);
        return mapper.ToDeals(entities);
    }

    public async Task<int> CreateAsync(IDeal deal, CancellationToken ct = default)
    {
        var entity = mapper.ToEntity(deal);
        await dealRepository.AddAsync(entity);
        await dealRepository.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(int dealId, IDeal deal, CancellationToken ct = default)
    {
        var existing = await dealRepository.GetByIdAsync(dealId)
            .OrNotFound($"Deal {dealId}");

        updater.Apply(existing, deal);
        dealRepository.Update(existing);
        await dealRepository.SaveChangesAsync();
    }

    public async Task DeleteAsync(int dealId, CancellationToken ct = default)
    {
        var existing = await dealRepository.GetByIdAsync(dealId);
        if (existing is null) return;

        dealRepository.Remove(existing);
        await dealRepository.SaveChangesAsync();
    }
}
