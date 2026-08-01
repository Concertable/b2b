using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Contracts.Errors;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.Kernel.Errors;
using Concertable.Kernel.Functional;

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

    public Task<Option<IDeal>> GetByIdAsync(int dealId, CancellationToken ct = default) =>
        dealRepository.GetByIdAsync(dealId, ct)
            .ToOption()
            .Map(mapper.ToDeal);

    public async Task<IReadOnlyList<IDeal>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default)
    {
        var entities = await dealRepository.GetByIdsAsync(dealIds, ct);
        return mapper.ToDeals(entities);
    }

    public UnitResult<ValidationErrors> Validate(IDeal deal) =>
        mapper.ToEntity(deal).Match(
            _ => UnitResult.Success<ValidationErrors>(),
            UnitResult.Failure);

    public Task<Result<int, CreateDealError>> CreateAsync(IDeal deal, CancellationToken ct = default) =>
        mapper.ToEntity(deal)
            .MapError(CreateDealError.Validation)
            .BindAsync(async (DealEntity entity) =>
            {
                await dealRepository.AddAsync(entity, ct);
                await dealRepository.SaveChangesAsync(ct);
                return Result.Success<int, CreateDealError>(entity.Id);
            });

    public async Task<UnitResult<UpdateDealError>> UpdateAsync(int dealId, IDeal deal, CancellationToken ct = default)
    {
        var existing = await dealRepository.GetByIdAsync(dealId, ct);
        if (existing is null)
            return UnitResult.Failure(UpdateDealError.NotFound(dealId));

        var update = updater.Apply(existing, deal).MapError(UpdateDealError.Validation);
        if (update.IsFailure)
            return update;

        dealRepository.Update(existing);
        await dealRepository.SaveChangesAsync(ct);
        return UnitResult.Success<UpdateDealError>();
    }

    public async Task DeleteAsync(int dealId, CancellationToken ct = default)
    {
        var existing = await dealRepository.GetByIdAsync(dealId, ct);
        if (existing is null) return;

        dealRepository.Remove(existing);
        await dealRepository.SaveChangesAsync(ct);
    }
}
