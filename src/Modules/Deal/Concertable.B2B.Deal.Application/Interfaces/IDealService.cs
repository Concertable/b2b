using Concertable.B2B.Deal.Application.Errors;
using Concertable.B2B.Deal.Contracts.Errors;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Application.Interfaces;

internal interface IDealService
{
    Task<Option<DealDto>> FindByIdAsync(int dealId, CancellationToken ct = default);
    Task<Result<DealDto, DealError>> GetByIdAsync(int dealId, CancellationToken ct = default);
    Task<IReadOnlyList<DealDto>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default);
    UnitResult<ValidationErrors> Validate(DealDto deal);
    Task<Result<int, CreateDealError>> CreateAsync(DealDto deal, CancellationToken ct = default);
    Task<UnitResult<UpdateDealError>> UpdateAsync(int dealId, DealDto deal, CancellationToken ct = default);
    Task DeleteAsync(int dealId, CancellationToken ct = default);
}
