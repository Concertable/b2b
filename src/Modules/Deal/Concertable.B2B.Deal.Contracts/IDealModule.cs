using Concertable.B2B.Deal.Contracts.Errors;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Contracts;

public interface IDealModule
{
    Task<Option<DealDto>> GetByIdAsync(int dealId, CancellationToken ct = default);
    Task<IReadOnlyList<DealDto>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default);
    UnitResult<ValidationErrors> Validate(DealDto deal);
    Task<Result<int, CreateDealError>> CreateAsync(DealDto deal, CancellationToken ct = default);
    Task<UnitResult<UpdateDealError>> UpdateAsync(int dealId, DealDto deal, CancellationToken ct = default);
}
