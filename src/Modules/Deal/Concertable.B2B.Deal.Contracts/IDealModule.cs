using Concertable.B2B.Deal.Contracts.Errors;
using Reunion.Errors;
using Reunion;

namespace Concertable.B2B.Deal.Contracts;

public interface IDealModule
{
    Task<Option<IDeal>> GetByIdAsync(int dealId, CancellationToken ct = default);
    Task<IReadOnlyList<IDeal>> GetByIdsAsync(IEnumerable<int> dealIds, CancellationToken ct = default);
    UnitResult<ValidationErrors> Validate(IDeal deal);
    Task<Result<int, CreateDealError>> CreateAsync(IDeal deal, CancellationToken ct = default);
    Task<UnitResult<UpdateDealError>> UpdateAsync(int dealId, IDeal deal, CancellationToken ct = default);
    Task DeleteAsync(int dealId, CancellationToken ct = default);
}
