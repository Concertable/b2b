namespace Concertable.B2B.Deal.Contracts;

public interface IDealPrivilegedReadRepository
{
    Task<DealDto?> GetByIdAsync(int dealId, CancellationToken ct = default);
}
