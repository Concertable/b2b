namespace Concertable.B2B.Deal.Contracts;

public interface IDealCommandFacts
{
    Task<DealDto?> GetByIdAsync(int dealId, CancellationToken ct = default);
}
