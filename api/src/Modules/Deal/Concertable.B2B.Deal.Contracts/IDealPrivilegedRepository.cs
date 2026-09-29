namespace Concertable.B2B.Deal.Contracts;

public interface IDealPrivilegedRepository
{
    Task<DealDto?> GetByIdAsync(int dealId, CancellationToken ct = default);
}
